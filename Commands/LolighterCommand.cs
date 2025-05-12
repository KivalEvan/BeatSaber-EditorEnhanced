using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using Zenject;

namespace EditorEnhanced.Commands;

internal static class Options
{
    private static float _colorOffset;
    private static float _colorSwap = 4.0f;
    private static float _colorBoostSwap = 8.0f;

    public static float ColorOffset
    {
        set => _colorOffset = value > -100.0f ? value : 0.0f;
        get => _colorOffset;
    }

    public static float ColorBoostSwap
    {
        set => _colorBoostSwap = value > 0.0f ? value : 8.0f;
        get => _colorBoostSwap;
    }

    public static float ColorSwap
    {
        set => _colorSwap = value > 0.0f ? value : 4.0f;
        get => _colorSwap;
    }

    public static bool UseBoostColor { set; get; } = false;
    public static bool NerfStrobes { set; get; } = false;
    public static bool IgnoreBomb { set; get; } = false;
}

internal static class Utils
{
    internal static int Swap(int value)
    {
        switch (value)
        {
            case 3: return 1;
            case 7: return 5;
            case 1: return 3;
            case 5: return 7;
            default: return 0;
        }
    }

    internal static int Inverse(int value)
    {
        if (value > 3)
            return value - 4; //Turn to blue
        return value + 4; //Turn to red
    }

    internal static void Shuffle<T>(this IList<T> list)
    {
        var rng = RandomNumberGenerator.Create();

        var n = list.Count;
        while (n > 1)
        {
            var box = new byte[1];
            do
            {
                rng.GetBytes(box);
            } while (!(box[0] < n * (byte.MaxValue / n)));

            var k = box[0] % n;
            n--;
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
}

public class LolighterCommand(
    SignalBus signalBus,
    BeatmapObjectsDataModel beatmapObjectsDataModel,
    BeatmapBasicEventsDataModel beatmapBasicEventsDataModel) : IBeatmapEditorCommandWithHistory
{
    private List<BasicEventEditorData> _newEventEditorData;
    private List<BasicEventEditorData> _previousEventEditorData;
    public bool shouldAddToHistory { get; private set; }

    public void Execute()
    {
        _previousEventEditorData = beatmapBasicEventsDataModel.GetAllEventsAsList();
        // Bunch of var to keep timing in check
        var last = new float();
        var time = new float[4];
        var notes = beatmapObjectsDataModel.GetNotesInterval(0, float.MaxValue).ToList();
        var selections = notes;
        float offset;
        float firstNote;

        //Light counter, stop at maximum.
        int count;

        // For laser speed
        var currentSpeed = 3;

        // Rhythm check
        float lastSpeed = 0;

        // To not light up Double twice
        float nextDouble = 0;

        // Slider stuff
        var firstSlider = false;
        var nextSlider = new float();
        var sliderLight = new List<BasicBeatmapEventType>
            { BasicBeatmapEventType.Event4, BasicBeatmapEventType.Event1, BasicBeatmapEventType.Event0 };
        var sliderIndex = 0;
        float sliderNoteCount = 0;
        var wasSlider = false;

        // Pattern for specific rhythm
        var pattern = new List<BasicBeatmapEventType>
        {
            BasicBeatmapEventType.Event0,
            BasicBeatmapEventType.Event1,
            BasicBeatmapEventType.Event2,
            BasicBeatmapEventType.Event3,
            BasicBeatmapEventType.Event4
        };
        var patternIndex = 0;
        var patternCount = 20;

        // The new events
        var eventTempo = new List<BasicEventEditorData>();

        // Is the section currently using Boost Event
        var boost = true;
        var boostSwap = Options.ColorBoostSwap;
        float boostIncrement = 0;

        // If double notes lights are on
        var doubleOn = false;

        // To make sure that slider doesn't apply as double
        var sliderTiming = new List<NoteEditorData>();

        // Order note, necessary if we're converting V3 bomb from notes
        notes = notes.OrderBy(o => o.beat).ToList();
        selections = selections.OrderBy(o => o.beat).ToList();

        ResetTimer();

        var found = false;

        // Place all spin/zoom/boost
        foreach (var note in notes)
        {
            var now = note.beat;
            time[0] = now;

            //Here we process Spin and Zoom
            if (now == firstNote &&
                time[1] == 0.0D) //If we are processing the first note, add spin + zoom + boost to it.
            {
                eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event8, now, 0, 0));
                eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event9, now, 0, 0));
                if (Options.UseBoostColor)
                {
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event5, now, 0, 0));
                    boost = false;
                }
            }
            else if (now >= Options.ColorOffset + Options.ColorSwap + offset &&
                     now > firstNote) //If we are reaching the next threshold of the timer
            {
                var calc = (int)((int)(now - offset) / Options.ColorSwap);

                for (var i = 0; i < calc; i++)
                {
                    offset += Options.ColorSwap;

                    //Add a spin at timer.
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event8, now, 0, 0));
                    if (count == 0) //Only add zoom every 2 spin.
                    {
                        eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event9, now, 0, 0));
                        count = 1;
                    }
                    else
                    {
                        count--;
                    }
                }
            }
            //If there's a quarter between two float parallel notes and timer didn't pass the check.
            else if (time[1] - time[2] == 0.25 && time[3] == time[2] && time[1] == now && 0 < offset)
            {
                eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event8, now, 0, 0));
            }

            // Boost Event
            if (now >= Options.ColorOffset + Options.ColorBoostSwap + boostIncrement && now > firstNote &&
                Options.UseBoostColor)
            {
                var calc = (int)((int)(now - boostIncrement) / Options.ColorBoostSwap);

                for (var i = 0; i < calc; i++)
                {
                    boostIncrement += Options.ColorBoostSwap;

                    if (boost)
                    {
                        eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event5, boostIncrement, 0,
                            0));
                        boost = false;
                    }
                    else
                    {
                        eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event5, boostIncrement, 1,
                            0));
                        boost = true;
                    }
                }
            }

            for (var i = 3; i > 0; i--) //Keep the timing of up to three notes before.
                time[i] = time[i - 1];
        }

        ResetTimer();

        // Find all sliders
        for (var i = 1; i < selections.Count; i++)
            // Between 1/8 and 0, same cut direction or dots
            if (notes[i].beat - notes[i - 1].beat <= 0.125 && notes[i].beat - notes[i - 1].beat > 0 &&
                (notes[i].cutDirection == notes[i - 1].cutDirection || notes[i].cutDirection == NoteCutDirection.Any ||
                 notes[i - 1].cutDirection == NoteCutDirection.Any))
            {
                sliderTiming.Add(notes[i - 1]);
                found = true;
            }
            else if (found)
            {
                sliderTiming.Add(notes[i - 1]);
                found = false;
            }

        foreach (var note in selections) //Process specific light using time.
        {
            var now = note.beat;
            time[0] = now;

            if (!Options.NerfStrobes && doubleOn && now != last) //Off event
            {
                if (now - last >= 1)
                {
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event0, now - (now - last) / 2,
                        0, 1));
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event1, now - (now - last) / 2,
                        0, 1));
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event4, now - (now - last) / 2,
                        0, 1));
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event2, now - (now - last) / 2,
                        0, 1));
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event3, now - (now - last) / 2,
                        0, 1));
                }
                else
                {
                    // Will be fused with some events, but we will sort that out later on.
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event0, now, 0, 1));
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event1, now, 0, 1));
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event4, now, 0, 1));
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event2, now, 0, 1));
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event3, now, 0, 1));
                }

                doubleOn = false;
            }

            //If not same note, same beat and not slider, apply once.
            if ((now == time[1] || (now - time[1] <= 0.02 && time[1] != time[2])) && time[1] != 0.0D && now != last &&
                !sliderTiming.Exists(e => e.beat == now))
            {
                var color = FindColor(notes.First().beat, time[0]);
                eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event0, now, color,
                    1)); //Back Top Laser
                eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event1, now, color,
                    1)); //Track Ring Neons
                eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event4, now, color,
                    1)); //Side Light
                eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event2, now, color,
                    1)); //Left Laser
                eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event3, now, color,
                    1)); //Right Laser


                // Laser speed based on rhythm
                if (time[0] - time[1] < 0.25)
                    currentSpeed = 7;
                else if (time[0] - time[1] >= 0.25 && time[0] - time[1] < 0.5)
                    currentSpeed = 5;
                else if (time[0] - time[1] >= 0.5 && time[0] - time[1] < 1)
                    currentSpeed = 3;
                else
                    currentSpeed = 1;

                eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event12, now, currentSpeed,
                    1)); //Left Rotation
                eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event13, now, currentSpeed,
                    1)); //Right Rotation

                doubleOn = true;
                last = now;
            }

            for (var i = 3; i > 0; i--) //Keep the timing of up to three notes before.
                time[i] = time[i - 1];
        }

        nextSlider = new float();

        // Convert quick light color swap
        if (Options.NerfStrobes)
        {
            float lastTimeTop = 100;
            float lastTimeNeon = 100;
            float lastTimeSide = 100;
            float lastTimeLeft = 100;
            float lastTimeRight = 100;

            foreach (var x in eventTempo)
                if (x.type == BasicBeatmapEventType.Event0)
                {
                    if (x.beat - lastTimeTop <= 0.5)
                    {
                        // x.value = Utils.Swap(x.value);
                    }

                    lastTimeTop = x.beat;
                }
                else if (x.type == BasicBeatmapEventType.Event1)
                {
                    if (x.beat - lastTimeNeon <= 0.5)
                    {
                        // x.value = Utils.Swap(x.value);
                    }

                    lastTimeNeon = x.beat;
                }
                else if (x.type == BasicBeatmapEventType.Event4)
                {
                    if (x.beat - lastTimeSide <= 0.5)
                    {
                        // x.value = Utils.Swap(x.value);
                    }

                    lastTimeSide = x.beat;
                }
                else if (x.type == BasicBeatmapEventType.Event2)
                {
                    if (x.beat - lastTimeLeft <= 0.5)
                    {
                        // x.value = Utils.Swap(x.value);
                    }

                    lastTimeLeft = x.beat;
                }
                else if (x.type == BasicBeatmapEventType.Event3)
                {
                    if (x.beat - lastTimeRight <= 0.5)
                    {
                        // x.value = Utils.Swap(x.value);
                    }

                    lastTimeRight = x.beat;
                }
        }

        ResetTimer();

        foreach (var note in selections) //Process all note using time.
        {
            time[0] = note.beat;

            if (wasSlider)
            {
                if (sliderNoteCount != 0)
                {
                    sliderNoteCount--;

                    for (var i = 3; i > 0; i--) //Keep the timing of up to three notes before.
                        time[i] = time[i - 1];

                    continue;
                }

                wasSlider = false;
            }

            if (firstSlider)
            {
                firstSlider = false;
                continue;
            }

            // Find the next double
            if (time[0] >= nextDouble)
                for (var i = selections.FindIndex(n => n == note); i < selections.Count - 1; i++)
                    if (i != 0)
                        if (selections[i].beat == selections[i - 1].beat)
                        {
                            nextDouble = selections[i].beat;
                            break;
                        }

            // Find the next slider (1/8 minimum) or chain
            if (time[0] >= nextSlider)
            {
                sliderNoteCount = 0;

                for (var i = selections.FindIndex(n => n == note); i < selections.Count - 1; i++)
                    if (i != 0 && i < selections.Count)
                    {
                        // Between 1/8 and 0, same cut direction or dots
                        if (selections[i].beat - selections[i - 1].beat <= 0.125 &&
                            selections[i].beat - selections[i - 1].beat > 0 &&
                            (selections[i].cutDirection == selections[i - 1].cutDirection ||
                             selections[i].cutDirection == NoteCutDirection.Any))
                        {
                            // Search for the last note of the slider
                            if (sliderNoteCount == 0)
                                // This is the first note of the slider
                                nextSlider = selections[i - 1].beat;

                            sliderNoteCount++;
                        }
                        else if (sliderNoteCount != 0)
                        {
                            break;
                        }
                    }
            }

            // It's the next slider or chain
            if (nextSlider == note.beat)
            {
                // Take a light between neon, side or backlight and strobes it via On/Flash
                if (sliderIndex == -1) sliderIndex = 2;

                // Place light
                var color = FindColor(notes.First().beat, time[0]);
                eventTempo.Add(BasicEventEditorData.CreateNew(sliderLight[sliderIndex], time[0], color - 2, 1));
                eventTempo.Add(BasicEventEditorData.CreateNew(sliderLight[sliderIndex], time[0] + 0.125f, color - 1,
                    1));
                eventTempo.Add(BasicEventEditorData.CreateNew(sliderLight[sliderIndex], time[0] + 0.25f, color - 2, 1));
                eventTempo.Add(BasicEventEditorData.CreateNew(sliderLight[sliderIndex], time[0] + 0.375f, color - 1,
                    1));
                eventTempo.Add(BasicEventEditorData.CreateNew(sliderLight[sliderIndex], time[0] + 0.5f, 0, 1));

                sliderIndex--;

                // Spin goes brrr
                eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event8, time[0], 0, 1));
                for (var i = 0; i < 8; i++)
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event8,
                        time[0] + 0.5f - 0.5f / 8f * i, 0, 1));

                wasSlider = true;
            }
            // Not a double
            else if (time[0] != nextDouble)
            {
                if (time[1] - time[2] >= lastSpeed + 0.02 || time[1] - time[2] <= lastSpeed - 0.02 ||
                    patternCount == 20) // New speed or 20 notes of the same pattern
                {
                    var old = BasicBeatmapEventType.Event0;
                    // New pattern
                    if (patternIndex != 0)
                        old = pattern[patternIndex - 1];
                    else
                        old = pattern[4];

                    do
                    {
                        pattern.Shuffle();
                    } while (pattern[0] == old);

                    patternIndex = 0;
                    patternCount = 0;
                }

                // Place the next light
                eventTempo.Add(BasicEventEditorData.CreateNew(pattern[patternIndex], time[0],
                    FindColor(notes.First().beat, time[0]), 1));

                // Speed based on rhythm
                if (time[0] - time[1] < 0.25)
                    currentSpeed = 7;
                else if (time[0] - time[1] >= 0.25 && time[0] - time[1] < 0.5)
                    currentSpeed = 5;
                else if (time[0] - time[1] >= 0.5 && time[0] - time[1] < 1)
                    currentSpeed = 3;
                else
                    currentSpeed = 1;

                // Add laser rotation if necessary
                if (pattern[patternIndex] == BasicBeatmapEventType.Event2)
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event12, time[0],
                        currentSpeed, 1));
                else if (pattern[patternIndex] == BasicBeatmapEventType.Event3)
                    eventTempo.Add(BasicEventEditorData.CreateNew(BasicBeatmapEventType.Event13, time[0],
                        currentSpeed, 1));

                // Place off event
                if (selections[selections.Count - 1].beat != note.beat)
                {
                    if (selections[selections.FindIndex(n => n == note) + 1].beat == nextDouble)
                    {
                        if (selections[selections.FindIndex(n => n == note) + 1].beat - time[0] <= 2)
                        {
                            var value = (selections[selections.FindIndex(n => n == note) + 1].beat -
                                         selections[selections.FindIndex(n => n == note)].beat) / 2;
                            eventTempo.Add(BasicEventEditorData.CreateNew(pattern[patternIndex],
                                selections[selections.FindIndex(n => n == note)].beat + value,
                                0, 1));
                        }
                    }
                    else
                    {
                        eventTempo.Add(BasicEventEditorData.CreateNew(pattern[patternIndex],
                            selections[selections.FindIndex(n => n == note) + 1].beat,
                            0, 1));
                    }
                }

                // Pattern have 5 notes in total (5 lights available)
                if (patternIndex < 4)
                    patternIndex++;
                else
                    patternIndex = 0;

                patternCount++;
                lastSpeed = time[0] - time[1];
            }

            for (var i = 3; i > 0; i--) //Keep the timing of up to three notes before.
                time[i] = time[i - 1];
        }

        eventTempo = eventTempo.OrderBy(o => o.beat).ToList();

        // Remove fused or move off event between
        eventTempo = RemoveFused(eventTempo);

        // Sort lights
        eventTempo = eventTempo.OrderBy(o => o.beat).ToList();

        _newEventEditorData = eventTempo;

        shouldAddToHistory = true;
        Redo();
        return;

        void ResetTimer() //Pretty much reset everything necessary.
        {
            firstNote = notes[0].beat;
            offset = firstNote;
            boostIncrement = firstNote;
            count = 1;
            for (var i = 0; i < 2; i++) time[i] = 0.0f;

            time[2] = 0.0f;
            time[3] = 0.0f;
        }
    }

    public void Undo()
    {
        beatmapBasicEventsDataModel.Clear();
        foreach (var ev in _previousEventEditorData) beatmapBasicEventsDataModel.Insert(ev);
        signalBus.Fire<BeatmapLevelUpdatedSignal>();
    }

    public void Redo()
    {
        beatmapBasicEventsDataModel.Clear();
        foreach (var ev in _newEventEditorData) beatmapBasicEventsDataModel.Insert(ev);
        signalBus.Fire<BeatmapLevelUpdatedSignal>();
    }

    public static List<BasicEventEditorData> RemoveFused(List<BasicEventEditorData> events)
    {
        var closest = 0f;

        // Get all fused events of a specific type
        for (var i = 0; i < events.Count; i++)
        {
            var e = events[i];

            var mapEvent = events.Find(o =>
                o.type == e.type && o.beat - e.beat >= -0.02 && o.beat - e.beat <= 0.02 && o != e);
            if (mapEvent == null) continue;
            var mapEvent2 = events.Find(o =>
                o.type == mapEvent.type &&
                o.beat - mapEvent.beat >= -0.02 && o.beat - mapEvent.beat <= 0.02 &&
                o != mapEvent);

            if (mapEvent2 == null) continue;
            var temp = events.FindLast(o =>
                o.beat < e.beat && e.beat > closest && o.value != 0);

            if (temp == null) continue;
            closest = temp.beat;
            if (mapEvent2.value == 0)
            {
                // Move off event between fused note and last note
                var idx = events.FindIndex(o =>
                    o.beat == mapEvent2.beat && o.value == mapEvent2.value &&
                    o.type == mapEvent2.type);
                var r = BasicEventEditorData.CreateNew(events[idx].type,
                    mapEvent2.beat - (mapEvent2.beat - closest) / 2, events[idx].value,
                    events[idx].floatValue);
                events.Remove(events[idx]);
                events.Insert(idx, r);
            }
            else
            {
                // Move off event between fused note and last note
                if (mapEvent.value == 0 ||
                    mapEvent.value == 4 ||
                    mapEvent.value == 8)
                {
                    var idx = events.FindIndex(o =>
                        o.beat == mapEvent.beat && o.value == mapEvent.value &&
                        o.type == mapEvent.type);
                    var r = BasicEventEditorData.CreateNew(events[idx].type,
                        mapEvent.beat - (mapEvent.beat - closest) / 2, events[idx].value,
                        events[idx].floatValue);
                    events.Remove(events[idx]);
                    events.Insert(idx, r);
                }
                else // Delete event
                {
                    events.RemoveAt(events.FindIndex(o =>
                        o.beat == mapEvent.beat && o.value == mapEvent.value &&
                        o.type == mapEvent.type));
                }
            }
        }

        return events;
    }

    public static int FindColor(float first, float current)
    {
        var color = 7;

        for (var i = 0;
             i < (current - first + Options.ColorOffset) / Options.ColorSwap;
             i++) //For each time that it need to swap.
            color = Utils.Inverse(color); //Swap color

        if (first == current) color = 3;

        return color;
    }
}