using System;
using System.Collections.Generic;
using System.Globalization;
using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo;
using EditorEnhanced.MotionPath.Configuration;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;

namespace EditorEnhanced.MotionPath;

internal sealed class MotionPathRenderer : IDisposable
{
   private static readonly Color UnfocusedColor = new(1f, 1f, 1f, 0.42f);
   private static readonly Color CurrentMarkerColor = new(1f, 0.94f, 0.55f, 1f);
   private static readonly Color EventMarkerColor = new(1f, 0.72f, 0.12f, 1f);
   private static readonly Color FutureColor = new(0.3f, 1f, 0.7f, 0.9f);
   private static readonly Color PastColor = new(1f, 0.3f, 0.12f, 0.9f);
   private static readonly Color TimelineBeatColor = new(1f, 1f, 1f, 0.95f);
   private static readonly Color TimelineSubBeatColor = new(1f, 1f, 1f, 0.7f);
   private static readonly Color TimelineUnfocusedColor = new(1f, 1f, 1f, 0.9f);

   private readonly Stack<RenderedTrack> _availableTracks = [];
   private readonly PluginConfig _config;
   private readonly MotionPathEventGizmoController _eventGizmoController;
   private readonly List<RenderedTrack> _tracks = [];
    private Material _lineMaterial;
     private Texture2D _lineTexture;
       private PathBeatMarkers _pathBeatMarkers;
      private Mesh _subBeatMarkerMesh;
    private GameObject _root;
    private Timeline _timeline;
    private EventBoxGroupType? _eventIndicatorGroupTypeFilter;

   public MotionPathRenderer(
      PluginConfig config,
      MotionPathEventGizmoController eventGizmoController)
   {
      _config = config;
      _eventGizmoController = eventGizmoController;
   }

    public void Dispose()
    {
       Clear();
       _pathBeatMarkers?.Dispose();
       if (_root != null) Object.Destroy(_root);
       if (_subBeatMarkerMesh != null) Object.Destroy(_subBeatMarkerMesh);
      if (_lineMaterial != null) Object.Destroy(_lineMaterial);
       if (_lineTexture != null) Object.Destroy(_lineTexture);
       foreach (var track in _availableTracks) track.Dispose();
       _availableTracks.Clear();
       _root = null;
       _subBeatMarkerMesh = null;
      _lineMaterial = null;
      _lineTexture = null;
       _pathBeatMarkers = null;
       _timeline = null;
   }

    public void Render(
       MotionPathPlan plan,
       float currentBeat,
       bool eventEditingEnabled,
       EventBoxGroupType? eventIndicatorGroupTypeFilter)
    {
        ClearTracks();
        SetEventIndicatorGroupTypeFilter(eventIndicatorGroupTypeFilter);
      if (!EnsureRoot()) return;
      foreach (var track in plan.Tracks)
      {
         var renderedTrack = AcquireTrack();
         renderedTrack.Configure(track);
         _tracks.Add(renderedTrack);
       }
       _timeline.EventEditingEnabled = eventEditingEnabled;
       _timeline.SetEventMarkerPlan(plan);
        UpdateClip(currentBeat);
     }

     public void RefreshPresentation(
        float currentBeat,
        bool eventEditingEnabled,
        EventBoxGroupType? eventIndicatorGroupTypeFilter)
     {
        SetEventIndicatorGroupTypeFilter(eventIndicatorGroupTypeFilter);
        if (_timeline != null) _timeline.EventEditingEnabled = eventEditingEnabled;
        UpdateClip(currentBeat);
     }

    public void SetEventIndicatorGroupTypeFilter(EventBoxGroupType? groupType)
    {
       if (_eventIndicatorGroupTypeFilter == groupType) return;
       _eventIndicatorGroupTypeFilter = groupType;
       _timeline?.SetEventIndicatorGroupTypeFilter(groupType);
    }

    public void UpdateClip(float currentBeat)
   {
      var minBeat = currentBeat - _config.MotionPath.GetBackwardRangeInBeats();
      var maxBeat = currentBeat + _config.MotionPath.GetForwardRangeInBeats();
      RenderedTrack firstFocused = null;
      RenderedTrack firstVisible = null;
      foreach (var track in _tracks)
      {
         track.Update(minBeat, maxBeat, currentBeat);
         if (firstVisible == null && track.HasVisibleRange(minBeat, maxBeat)) firstVisible = track;
         if (firstFocused == null && track.IsFocused) firstFocused = track;
      }

       var timelineTrack = firstFocused ?? firstVisible;
       _pathBeatMarkers?.Update(_tracks, minBeat, maxBeat);
        if (timelineTrack == null) _timeline?.Release();
         else _timeline?.Update(timelineTrack, minBeat, maxBeat, currentBeat);
    }

    public void SubmitMarkerBatches() => _pathBeatMarkers?.Submit();

   public void Clear()
   {
      ClearTracks();
   }

   private bool EnsureRoot()
   {
      if (_root != null) return true;
      var shader = Shader.Find("Sprites/Default");
      if (shader == null) return false;
       _root = new GameObject("EditorEnhancedMotionPaths");
       _lineTexture = CreateLineTexture();
       _lineMaterial = new Material(shader) { mainTexture = _lineTexture };
       var sphereMarkerMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
       _subBeatMarkerMesh = CreateSubBeatMarkerMesh();
          _pathBeatMarkers = new PathBeatMarkers(
             _root.transform,
             _config,
             sphereMarkerMesh,
             _subBeatMarkerMesh);
        _timeline = new Timeline(
            _root.transform,
            _lineMaterial,
            Resources.GetBuiltinResource<Font>("Arial.ttf"),
            _config,
              _eventGizmoController);
       _timeline.SetEventIndicatorGroupTypeFilter(_eventIndicatorGroupTypeFilter);
       return true;
   }

   private RenderedTrack AcquireTrack()
   {
      if (_availableTracks.Count > 0) return _availableTracks.Pop();
       var trackObject = new GameObject("MotionPath");
       trackObject.transform.SetParent(_root.transform, false);
        return new RenderedTrack(trackObject, _lineMaterial, _config);
   }

   private static Texture2D CreateLineTexture()
   {
      const int height = 16;
      var texture = new Texture2D(1, height, TextureFormat.RGBA32, true, true)
      {
         name = "MotionPathAntiAliasedLine",
         filterMode = FilterMode.Trilinear,
         wrapMode = TextureWrapMode.Clamp,
         anisoLevel = 4
      };
      var pixels = new Color[height];
      for (var pixelIndex = 0; pixelIndex < height; pixelIndex++)
      {
         var across = pixelIndex / (height - 1f);
         var edgeDistance = Mathf.Min(across, 1f - across) * 2f;
         var alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edgeDistance * 3f));
         pixels[pixelIndex] = new Color(1f, 1f, 1f, alpha);
      }
      texture.SetPixels(pixels);
      texture.Apply(true, true);
      return texture;
   }

   private static Mesh CreateSubBeatMarkerMesh()
   {
      var mesh = new Mesh { name = "MotionPathSubBeatMarker" };
      mesh.vertices =
      [
         new Vector3(-0.5f, -0.5f, -0.5f),
         new Vector3(0.5f, -0.5f, -0.5f),
         new Vector3(0.5f, 0.5f, -0.5f),
         new Vector3(-0.5f, 0.5f, -0.5f),
         new Vector3(0f, 0f, 0.5f)
      ];
      mesh.triangles =
      [
         0, 1, 4,
         1, 2, 4,
         2, 3, 4,
         3, 0, 4,
         0, 3, 2,
         0, 2, 1
      ];
      mesh.RecalculateNormals();
      mesh.RecalculateBounds();
      return mesh;
   }

    private void ClearTracks()
    {
       _pathBeatMarkers?.Release();
       _timeline?.Release();
      _eventGizmoController.Clear();
      foreach (var track in _tracks)
      {
         track.Release();
         _availableTracks.Push(track);
      }
      _tracks.Clear();
   }

       private sealed class RenderedTrack
       {
          private readonly PluginConfig _config;
        private readonly List<LineSection> _futureLines = [];
        private readonly Material _lineMaterial;
        private readonly List<LineSection> _pastLines = [];
       private IReadOnlyList<MotionPathEventPoint> _eventPoints;
         private IReadOnlyList<MotionPathSample> _samples;
         private IReadOnlyList<float> _stepBeats;
         private Transform _source;
         private IReadOnlyList<int> _visualSampleIndices;

        public RenderedTrack(
           GameObject gameObject,
           Material lineMaterial,
           PluginConfig config)
       {
          GameObject = gameObject;
          _lineMaterial = lineMaterial;
          _pastLines.Add(CreateLine("Past"));
          _futureLines.Add(CreateLine("Future"));
           _config = config;
      }

       public GameObject GameObject { get; }
       public bool IsFocused { get; private set; }

      public void Configure(MotionPathTrack track)
      {
         _source = track.Source;
          _samples = track.Samples;
            _eventPoints = track.EventPoints;
            _stepBeats = track.StepBeats;
            IsFocused = track.IsFocused;
            _visualSampleIndices = track.VisualSampleIndices;
            GameObject.name = track.IsFocused ? "MotionPathFocused" : "MotionPathUnfocused";
          GameObject.SetActive(true);
       }

      public void Release()
      {
          _source = null;
          _samples = null;
            _eventPoints = null;
            _stepBeats = null;
            _visualSampleIndices = null;
           IsFocused = false;
           ReleaseLines(_pastLines);
           ReleaseLines(_futureLines);
           ReleaseLineBuffers(_pastLines);
           ReleaseLineBuffers(_futureLines);
           GameObject.SetActive(false);
       }

       public void Dispose()
       {
          foreach (var line in _pastLines) line.Dispose();
          foreach (var line in _futureLines) line.Dispose();
          _pastLines.Clear();
          _futureLines.Clear();
       }

        public void Update(float minBeat, float maxBeat, float currentBeat)
       {
           if (!_config.MotionPath.GetShowPathLines())
           {
              ReleaseLines(_pastLines);
              ReleaseLines(_futureLines);
              return;
           }

           if (!TryGetVisibleRange(minBeat, maxBeat, out var visibleMinBeat, out var visibleMaxBeat))
          {
             ReleaseLines(_pastLines);
             ReleaseLines(_futureLines);
             return;
          }

           var splitBeat = Mathf.Clamp(currentBeat, visibleMinBeat, visibleMaxBeat);
           var lineWidth = IsFocused
              ? _config.MotionPath.GetFocusedLineWidth()
              : _config.MotionPath.GetUnfocusedLineWidth();
           SetLineSections(
              _pastLines,
              visibleMinBeat,
              splitBeat,
              splitBeat,
              visibleMinBeat,
              lineWidth,
              IsFocused ? PastColor : UnfocusedColor);
           SetLineSections(
              _futureLines,
              splitBeat,
              visibleMaxBeat,
              splitBeat,
              visibleMaxBeat,
              lineWidth,
              IsFocused ? FutureColor : UnfocusedColor);
      }

      public bool TryGetVisibleRange(float minBeat, float maxBeat, out float visibleMinBeat, out float visibleMaxBeat)
      {
         visibleMinBeat = 0f;
         visibleMaxBeat = 0f;
         if (_source == null || _samples == null || _samples.Count == 0) return false;

         visibleMinBeat = Mathf.Max(minBeat, _samples[0].Beat);
         visibleMaxBeat = Mathf.Min(maxBeat, _samples[_samples.Count - 1].Beat);
         return visibleMinBeat <= visibleMaxBeat;
      }

      public bool HasVisibleRange(float minBeat, float maxBeat) =>
         TryGetVisibleRange(minBeat, maxBeat, out _, out _);

       public bool TryGetPosition(float beat, out Vector3 position)
       {
          position = Vector3.zero;
          if (_samples == null || _samples.Count == 0 || beat < _samples[0].Beat || beat > _samples[_samples.Count - 1].Beat)
             return false;
           if (_eventPoints != null)
           {
              var eventPointIndex = FindFirstEventPointAtOrAfter(beat);
              if (eventPointIndex < _eventPoints.Count && _eventPoints[eventPointIndex].Beat == beat)
              {
                 position = _eventPoints[eventPointIndex].Position;
                 return true;
              }
           }

          var upperSampleIndex = FindFirstSampleAtOrAfter(beat);
         if (upperSampleIndex == 0)
         {
            position = _samples[0].Position;
            return true;
         }

         if (upperSampleIndex >= _samples.Count)
         {
            position = _samples[_samples.Count - 1].Position;
            return true;
         }

         var upperSample = _samples[upperSampleIndex];
         if (upperSample.Beat == beat)
         {
            position = upperSample.Position;
            return true;
         }

         var lowerSample = _samples[upperSampleIndex - 1];
         position = Vector3.LerpUnclamped(
            lowerSample.Position,
            upperSample.Position,
            Mathf.InverseLerp(lowerSample.Beat, upperSample.Beat, beat));
          return true;
       }

       public bool TryGetDirection(
          float beat,
          float minBeat,
          float maxBeat,
          out Vector3 direction)
       {
          const float minimumDirectionSqrMagnitude = 0.000001f;
          direction = Vector3.forward;
          if (_samples == null || _samples.Count < 2) return false;

          var upperSampleIndex = FindFirstSampleAtOrAfter(beat);
          var beforeSampleIndex = upperSampleIndex - 1;
          var afterSampleIndex = upperSampleIndex;
          if (upperSampleIndex < _samples.Count
              && Mathf.Approximately(_samples[upperSampleIndex].Beat, beat))
          {
             afterSampleIndex++;
          }

          beforeSampleIndex = Mathf.Clamp(beforeSampleIndex, 0, _samples.Count - 1);
          afterSampleIndex = Mathf.Clamp(afterSampleIndex, 0, _samples.Count - 1);
          var beforeBeat = Mathf.Clamp(_samples[beforeSampleIndex].Beat, minBeat, maxBeat);
          var afterBeat = Mathf.Clamp(_samples[afterSampleIndex].Beat, minBeat, maxBeat);
          if (!TryGetPosition(beforeBeat, out var beforePosition)
              || !TryGetPosition(afterBeat, out var afterPosition))
             return false;

          var pathDelta = afterPosition - beforePosition;
          var pathDeltaSqrMagnitude = pathDelta.sqrMagnitude;
          if (pathDeltaSqrMagnitude <= minimumDirectionSqrMagnitude
              || float.IsNaN(pathDeltaSqrMagnitude)
              || float.IsInfinity(pathDeltaSqrMagnitude))
             return false;

          direction = pathDelta / Mathf.Sqrt(pathDeltaSqrMagnitude);
          return true;
       }

        private LineSection CreateLine(string name)
       {
          var lineObject = new GameObject(name);
          lineObject.transform.SetParent(GameObject.transform, false);
          var line = lineObject.AddComponent<LineRenderer>();
          line.useWorldSpace = true;
          line.alignment = LineAlignment.View;
          line.numCapVertices = 4;
          line.numCornerVertices = 4;
          line.textureMode = LineTextureMode.Stretch;
          line.sharedMaterial = _lineMaterial;
           return new LineSection(line);
        }

         private static void ConfigureLine(LineSection line, float width, Color startColor, Color endColor)
         {
            line.Renderer.widthMultiplier = width;
            line.Renderer.startColor = startColor;
           line.Renderer.endColor = endColor;
       }

        private static void ClearLine(LineSection line)
        {
           line.Renderer.positionCount = 0;
        }

        private static void ReleaseLines(IReadOnlyList<LineSection> lines) => ReleaseLinesFrom(lines, 0);

        private static void ReleaseLineBuffers(IReadOnlyList<LineSection> lines)
        {
           foreach (var line in lines) line.ReleasePositionBuffer();
        }

        private static void ReleaseLinesFrom(IReadOnlyList<LineSection> lines, int firstUnusedLine)
       {
          for (var lineIndex = firstUnusedLine; lineIndex < lines.Count; lineIndex++)
          {
             ClearLine(lines[lineIndex]);
              lines[lineIndex].Renderer.gameObject.SetActive(false);
          }
       }

       private int FindFirstSampleAtOrAfter(float beat)
      {
         var low = 0;
         var high = _samples.Count;
         while (low < high)
         {
            var middle = low + (high - low) / 2;
            if (_samples[middle].Beat < beat) low = middle + 1;
            else high = middle;
         }
          return low;
       }

       private int FindFirstEventPointAtOrAfter(float beat)
       {
          var low = 0;
          var high = _eventPoints.Count;
          while (low < high)
          {
             var middle = low + (high - low) / 2;
             if (_eventPoints[middle].Beat < beat) low = middle + 1;
             else high = middle;
          }
          return low;
       }

       private int FindFirstVisualSampleAtOrAfter(float beat) =>
          FindFirstVisualSample(beat, false);

       private int FindFirstVisualSampleAfter(float beat) =>
          FindFirstVisualSample(beat, true);

       private int FindFirstVisualSample(float beat, bool strictlyAfter)
       {
          var low = 0;
          var high = _visualSampleIndices.Count;
          while (low < high)
          {
             var middle = low + (high - low) / 2;
             var sampleBeat = _samples[_visualSampleIndices[middle]].Beat;
             if (sampleBeat < beat || strictlyAfter && sampleBeat == beat) low = middle + 1;
             else high = middle;
          }

          return low;
       }

        private void SetLineSections(
            List<LineSection> lines,
           float startBeat,
           float endBeat,
           float opaqueBeat,
           float transparentBeat,
           float width,
           Color color)
       {
          var lineCount = 0;
          var sectionStartBeat = startBeat;
          if (_stepBeats != null)
          {
             foreach (var stepBeat in _stepBeats)
             {
                if (stepBeat <= sectionStartBeat) continue;
                if (stepBeat > endBeat) break;
                 if (SetLinePositions(
                        GetLine(lines, lineCount),
                        sectionStartBeat,
                        stepBeat,
                        false,
                        opaqueBeat,
                        transparentBeat,
                        width,
                        color))
                    lineCount++;
                sectionStartBeat = stepBeat;
             }
          }

           if (SetLinePositions(
                  GetLine(lines, lineCount),
                  sectionStartBeat,
                  endBeat,
                  true,
                  opaqueBeat,
                  transparentBeat,
                  width,
                  color))
              lineCount++;
          ReleaseLinesFrom(lines, lineCount);
       }

        private LineSection GetLine(List<LineSection> lines, int lineIndex)
       {
          while (lines.Count <= lineIndex) lines.Add(CreateLine(lines == _pastLines ? "Past" : "Future"));
          return lines[lineIndex];
       }

       private bool SetLinePositions(
           LineSection line,
            float startBeat,
           float endBeat,
           bool includeEndPosition,
           float opaqueBeat,
           float transparentBeat,
           float width,
           Color color)
       {
          if (startBeat >= endBeat || !TryGetPosition(startBeat, out var startPosition))
          {
             ClearLine(line);
              line.Renderer.gameObject.SetActive(false);
             return false;
          }

           var firstInteriorSample = FindFirstVisualSampleAfter(startBeat);
           var firstSampleAtEnd = FindFirstVisualSampleAtOrAfter(endBeat);
           var interiorSampleCount = firstSampleAtEnd - firstInteriorSample;
           var endpointCount = includeEndPosition ? 2 : 1;
           if (interiorSampleCount > int.MaxValue - endpointCount)
           {
              ClearLine(line);
              line.Renderer.gameObject.SetActive(false);
              return false;
           }

           var positionCount = interiorSampleCount + endpointCount;
          if (positionCount < 2)
          {
             ClearLine(line);
              line.Renderer.gameObject.SetActive(false);
             return false;
          }

          var endPosition = Vector3.zero;
          if (includeEndPosition && !TryGetPosition(endBeat, out endPosition))
          {
             ClearLine(line);
              line.Renderer.gameObject.SetActive(false);
             return false;
          }

             var lineEndBeat = includeEndPosition
                ? endBeat
                : _samples[_visualSampleIndices[firstInteriorSample + interiorSampleCount - 1]].Beat;
            ConfigureLine(
               line,
               width,
               GetFadedColor(color, startBeat, transparentBeat, opaqueBeat),
               GetFadedColor(color, lineEndBeat, transparentBeat, opaqueBeat));
            line.Renderer.gameObject.SetActive(true);
           line.SetPositions(
              positionCount,
               startPosition,
               _samples,
               _visualSampleIndices,
               firstInteriorSample,
              interiorSampleCount,
              includeEndPosition,
              endPosition);
            return true;
         }

         private static Color GetFadedColor(
            Color color,
            float beat,
            float transparentBeat,
            float opaqueBeat)
         {
            color.a *= Mathf.SmoothStep(
               0f,
               1f,
               Mathf.InverseLerp(transparentBeat, opaqueBeat, beat));
            return color;
         }

        private sealed class LineSection : IDisposable
        {
           private NativeArray<Vector3> _positions;

           public LineSection(LineRenderer renderer)
           {
              Renderer = renderer;
           }

           public LineRenderer Renderer { get; }

            public void Dispose()
            {
               ReleasePositionBuffer();
            }

            public void ReleasePositionBuffer()
            {
               if (!_positions.IsCreated) return;
               _positions.Dispose();
               _positions = default;
            }

           public void SetPositions(
              int positionCount,
              Vector3 startPosition,
               IReadOnlyList<MotionPathSample> samples,
               IReadOnlyList<int> visualSampleIndices,
               int firstInteriorSample,
              int interiorSampleCount,
              bool includeEndPosition,
              Vector3 endPosition)
           {
               EnsurePositionBuffer(positionCount);
               FillPositions(
                  _positions,
                   startPosition,
                   samples,
                   visualSampleIndices,
                   firstInteriorSample,
                   interiorSampleCount,
                   includeEndPosition,
                   endPosition);
               Renderer.positionCount = positionCount;
               Renderer.SetPositions(new NativeSlice<Vector3>(_positions, 0, positionCount));
           }

           private void EnsurePositionBuffer(int positionCount)
           {
              if (_positions.IsCreated && _positions.Length >= positionCount) return;
               var capacity = _positions.IsCreated ? _positions.Length : 1;
               while (capacity < positionCount)
                  capacity = capacity > positionCount / 2 ? positionCount : capacity * 2;
              if (_positions.IsCreated) _positions.Dispose();
              _positions = new NativeArray<Vector3>(
                 capacity,
                 Allocator.Persistent,
                 NativeArrayOptions.UninitializedMemory);
           }

           private static void FillPositions(
              NativeArray<Vector3> positions,
               Vector3 startPosition,
               IReadOnlyList<MotionPathSample> samples,
               IReadOnlyList<int> visualSampleIndices,
               int firstInteriorSample,
              int interiorSampleCount,
              bool includeEndPosition,
              Vector3 endPosition)
           {
               positions[0] = startPosition;
               for (var sampleIndex = 0; sampleIndex < interiorSampleCount; sampleIndex++)
                  positions[sampleIndex + 1] = samples[visualSampleIndices[firstInteriorSample + sampleIndex]].Position;
              if (includeEndPosition) positions[interiorSampleCount + 1] = endPosition;
           }
         }
      }

    private sealed class PathBeatMarkers : IDisposable
    {
       private readonly InstancedMarkerBatch _beatMarkerBatch;
       private readonly List<Marker> _beatFallbackMarkers = [];
       private readonly List<Matrix4x4> _beatMatrices = [];
       private readonly List<TrackMarkerRange> _beatRanges = [];
       private readonly PluginConfig _config;
       private readonly Transform _parent;
       private readonly InstancedMarkerBatch _subBeatMarkerBatch;
       private readonly List<Marker> _subBeatFallbackMarkers = [];
       private readonly List<Matrix4x4> _subBeatMatrices = [];
       private readonly Mesh _subBeatMarkerMesh;
       private readonly List<TrackMarkerRange> _subBeatRanges = [];
       private bool _beatFallbackActive;
       private float _beatMarkerSize = float.NaN;
       private bool _beatMarkersVisible;
       private Transform _fallbackRoot;
       private bool _subBeatFallbackActive;
       private int _subBeatDivisions;
       private float _subBeatMarkerSize = float.NaN;
       private bool _subBeatMarkersVisible;

       public PathBeatMarkers(
          Transform parent,
          PluginConfig config,
          Mesh sphereMarkerMesh,
          Mesh subBeatMarkerMesh)
       {
          _parent = parent;
          _config = config;
          _subBeatMarkerMesh = subBeatMarkerMesh;
          _beatMarkerBatch = new InstancedMarkerBatch(
             sphereMarkerMesh,
             TimelineBeatColor,
             "MotionPathBeatMarkerMaterial");
          _subBeatMarkerBatch = new InstancedMarkerBatch(
             subBeatMarkerMesh,
             TimelineSubBeatColor,
             "MotionPathSubBeatMarkerMaterial");
       }

       public void Dispose()
       {
          Release();
          _beatMarkerBatch.Dispose();
          _subBeatMarkerBatch.Dispose();
          if (_fallbackRoot != null) Object.Destroy(_fallbackRoot.gameObject);
          _fallbackRoot = null;
       }

       public void Update(IReadOnlyList<RenderedTrack> tracks, float minBeat, float maxBeat)
       {
          UpdateBeatMarkers(tracks, minBeat, maxBeat);
          UpdateSubBeatMarkers(tracks, minBeat, maxBeat);
       }

       public void Submit()
       {
          SubmitBeatMarkers();
          SubmitSubBeatMarkers();
       }

       public void Release()
       {
          ReleaseMarkers(_beatFallbackMarkers);
          ReleaseMarkers(_subBeatFallbackMarkers);
          _beatMatrices.Clear();
          _subBeatMatrices.Clear();
          _beatRanges.Clear();
          _subBeatRanges.Clear();
          _beatFallbackActive = false;
          _beatMarkersVisible = false;
          _beatMarkerSize = float.NaN;
          _subBeatFallbackActive = false;
          _subBeatMarkersVisible = false;
          _subBeatMarkerSize = float.NaN;
          _subBeatDivisions = 0;
       }

       private void UpdateBeatMarkers(IReadOnlyList<RenderedTrack> tracks, float minBeat, float maxBeat)
       {
          var markersVisible = _config.MotionPath.GetShowBeatMarkers();
          var markerSize = _config.MotionPath.GetBeatMarkerSize();
          var rangeChanged = UpdateRanges(_beatRanges, tracks, minBeat, maxBeat, 1);
          var markerStateChanged = rangeChanged
             || markersVisible != _beatMarkersVisible
             || !Mathf.Approximately(markerSize, _beatMarkerSize);
          _beatMarkersVisible = markersVisible;
          _beatMarkerSize = markerSize;
          if (!markersVisible)
          {
             ReleaseMarkers(_beatFallbackMarkers);
             _beatFallbackActive = false;
             return;
          }

           if (markerStateChanged) RebuildBeatMatrices(markerSize);
           if (markerStateChanged && _beatFallbackActive)
              RebuildBeatFallbackMarkers(markerSize);
       }

       private void SubmitBeatMarkers()
       {
          if (!_beatMarkersVisible) return;
          if (_beatMarkerBatch.TrySubmit(_beatMatrices))
          {
             ReleaseMarkers(_beatFallbackMarkers);
             _beatFallbackActive = false;
             return;
          }

          if (!_beatFallbackActive) RebuildBeatFallbackMarkers(_beatMarkerSize);
          _beatFallbackActive = true;
       }

       private void UpdateSubBeatMarkers(
          IReadOnlyList<RenderedTrack> tracks,
          float minBeat,
          float maxBeat)
       {
          var subdivisions = _config.MotionPath.GetSubBeatDivisions();
          var markersVisible = _config.MotionPath.GetShowSubBeatMarkers() && subdivisions > 1;
          var markerSize = _config.MotionPath.GetSubBeatMarkerSize();
          var rangeChanged = UpdateRanges(_subBeatRanges, tracks, minBeat, maxBeat, subdivisions);
          var markerStateChanged = rangeChanged
             || markersVisible != _subBeatMarkersVisible
             || subdivisions != _subBeatDivisions
             || !Mathf.Approximately(markerSize, _subBeatMarkerSize);
          _subBeatMarkersVisible = markersVisible;
          _subBeatMarkerSize = markerSize;
          _subBeatDivisions = subdivisions;
          if (!markersVisible)
          {
             ReleaseMarkers(_subBeatFallbackMarkers);
             _subBeatFallbackActive = false;
             return;
          }

           if (markerStateChanged) RebuildSubBeatMatrices(markerSize, subdivisions);
           if (markerStateChanged && _subBeatFallbackActive)
              RebuildSubBeatFallbackMarkers(markerSize, subdivisions);
       }

       private void SubmitSubBeatMarkers()
       {
          if (!_subBeatMarkersVisible) return;
          if (_subBeatMarkerBatch.TrySubmit(_subBeatMatrices))
          {
             ReleaseMarkers(_subBeatFallbackMarkers);
             _subBeatFallbackActive = false;
             return;
          }

          if (!_subBeatFallbackActive)
             RebuildSubBeatFallbackMarkers(_subBeatMarkerSize, _subBeatDivisions);
          _subBeatFallbackActive = true;
       }

       private void RebuildBeatMatrices(float markerSize)
       {
          _beatMatrices.Clear();
          foreach (var range in _beatRanges)
             for (var beat = range.Start; beat <= range.End; beat++)
                if (range.Track.TryGetPosition(beat, out var position))
                   _beatMatrices.Add(Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * markerSize));
       }

       private void RebuildSubBeatMatrices(float markerSize, int subdivisions)
       {
          _subBeatMatrices.Clear();
          foreach (var range in _subBeatRanges)
          {
             var directionMinBeat = range.Start / (float)subdivisions;
             var directionMaxBeat = range.End / (float)subdivisions;
             for (var subBeat = range.Start; subBeat <= range.End; subBeat++)
             {
                if (subBeat % subdivisions == 0) continue;
                var beat = subBeat / (float)subdivisions;
                if (!range.Track.TryGetPosition(beat, out var position)) continue;
                var rotation = range.Track.TryGetDirection(
                   beat,
                   directionMinBeat,
                   directionMaxBeat,
                   out var direction)
                   ? Quaternion.LookRotation(direction)
                   : Quaternion.identity;
                _subBeatMatrices.Add(Matrix4x4.TRS(position, rotation, Vector3.one * markerSize));
             }
          }
       }

       private void RebuildBeatFallbackMarkers(float markerSize)
       {
          var markerCount = 0;
          foreach (var range in _beatRanges)
          {
             for (var beat = range.Start; beat <= range.End; beat++)
             {
                if (!range.Track.TryGetPosition(beat, out var position)) continue;
                EnsureBeatFallbackMarkerCount(markerCount + 1);
                _beatFallbackMarkers[markerCount++].Set(position, markerSize, TimelineBeatColor);
             }
          }

          ReleaseMarkersFrom(_beatFallbackMarkers, markerCount);
       }

       private void RebuildSubBeatFallbackMarkers(float markerSize, int subdivisions)
       {
          var markerCount = 0;
          foreach (var range in _subBeatRanges)
          {
             var directionMinBeat = range.Start / (float)subdivisions;
             var directionMaxBeat = range.End / (float)subdivisions;
             for (var subBeat = range.Start; subBeat <= range.End; subBeat++)
             {
                if (subBeat % subdivisions == 0) continue;
                var beat = subBeat / (float)subdivisions;
                if (!range.Track.TryGetPosition(beat, out var position)) continue;
                var rotation = range.Track.TryGetDirection(
                   beat,
                   directionMinBeat,
                   directionMaxBeat,
                   out var direction)
                   ? Quaternion.LookRotation(direction)
                   : Quaternion.identity;
                EnsureSubBeatFallbackMarkerCount(markerCount + 1);
                _subBeatFallbackMarkers[markerCount++].Set(
                   position,
                   markerSize,
                   TimelineSubBeatColor,
                   rotation);
             }
          }

          ReleaseMarkersFrom(_subBeatFallbackMarkers, markerCount);
       }

       private static bool UpdateRanges(
          List<TrackMarkerRange> ranges,
          IReadOnlyList<RenderedTrack> tracks,
          float minBeat,
          float maxBeat,
          int subdivisions)
       {
          var changed = ranges.Count != tracks.Count;
          for (var trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
          {
             var track = tracks[trackIndex];
             var start = 1;
             var end = 0;
             if (track.TryGetVisibleRange(minBeat, maxBeat, out var visibleMinBeat, out var visibleMaxBeat))
             {
                start = Mathf.CeilToInt(visibleMinBeat * subdivisions);
                end = Mathf.FloorToInt(visibleMaxBeat * subdivisions);
             }

             var range = new TrackMarkerRange(track, start, end);
             if (trackIndex == ranges.Count)
             {
                ranges.Add(range);
                changed = true;
             }
             else if (!ranges[trackIndex].Equals(range))
             {
                ranges[trackIndex] = range;
                changed = true;
             }
          }

          return changed;
       }

       private void EnsureBeatFallbackMarkerCount(int count)
       {
          while (_beatFallbackMarkers.Count < count)
             _beatFallbackMarkers.Add(new Marker(
                GetFallbackRoot(),
                GizmoAssets.DefaultMaterial,
                "BeatMarker",
                PrimitiveType.Sphere));
       }

       private void EnsureSubBeatFallbackMarkerCount(int count)
       {
          while (_subBeatFallbackMarkers.Count < count)
             _subBeatFallbackMarkers.Add(new Marker(
                GetFallbackRoot(),
                GizmoAssets.DefaultMaterial,
                "SubBeatMarker",
                _subBeatMarkerMesh));
       }

       private Transform GetFallbackRoot()
       {
          if (_fallbackRoot != null) return _fallbackRoot;
          var markerObject = new GameObject("MotionPathBeatMarkerFallback");
          markerObject.transform.SetParent(_parent, false);
          _fallbackRoot = markerObject.transform;
          return _fallbackRoot;
       }

       private static void ReleaseMarkers(List<Marker> markers) => ReleaseMarkersFrom(markers, 0);

       private static void ReleaseMarkersFrom(List<Marker> markers, int firstUnusedIndex)
       {
          for (var markerIndex = firstUnusedIndex; markerIndex < markers.Count; markerIndex++)
             markers[markerIndex].Release();
       }

       private readonly struct TrackMarkerRange : IEquatable<TrackMarkerRange>
       {
          public TrackMarkerRange(RenderedTrack track, int start, int end)
          {
             Track = track;
             Start = start;
             End = end;
          }

          public RenderedTrack Track { get; }
          public int Start { get; }
          public int End { get; }

          public bool Equals(TrackMarkerRange other) =>
             ReferenceEquals(Track, other.Track) && Start == other.Start && End == other.End;

          public override bool Equals(object obj) => obj is TrackMarkerRange other && Equals(other);

          public override int GetHashCode()
          {
             unchecked
             {
                var hash = Track == null ? 0 : Track.GetHashCode();
                hash = hash * 31 + Start;
                return hash * 31 + End;
             }
          }
       }
    }

    private sealed class InstancedMarkerBatch : IDisposable
    {
       private const int MaximumMatrixInstances = 511;
       private static bool _instancingFailureLogged;

       private readonly Mesh _mesh;
       private readonly string _name;
       private Material _material;
       private bool _unavailable;

       public InstancedMarkerBatch(Mesh mesh, Color color, string name)
       {
          _mesh = mesh;
          _name = name;
          if (_mesh == null || GizmoAssets.DefaultMaterial == null)
          {
             _unavailable = true;
             return;
          }

          _material = new Material(GizmoAssets.DefaultMaterial) { name = name, enableInstancing = true };
          if (_material.HasProperty("_Color")) _material.SetColor("_Color", color);
       }

       public void Dispose()
       {
          if (_material != null) Object.Destroy(_material);
          _material = null;
       }

       public bool TrySubmit(List<Matrix4x4> matrices)
       {
          if (matrices.Count == 0) return true;
          if (_unavailable || _material == null || _mesh == null || !SystemInfo.supportsInstancing)
             return false;

          try
          {
             var renderParameters = new RenderParams(_material)
             {
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                lightProbeUsage = LightProbeUsage.Off,
                reflectionProbeUsage = ReflectionProbeUsage.Off,
                motionVectorMode = MotionVectorGenerationMode.ForceNoMotion
             };
             for (var firstInstance = 0; firstInstance < matrices.Count; firstInstance += MaximumMatrixInstances)
             {
                var instanceCount = Math.Min(MaximumMatrixInstances, matrices.Count - firstInstance);
                Graphics.RenderMeshInstanced(
                   in renderParameters,
                   _mesh,
                   0,
                   matrices,
                   instanceCount,
                   firstInstance);
             }

             return true;
          }
          catch (InvalidOperationException exception)
          {
             _unavailable = true;
             if (!_instancingFailureLogged)
             {
                Debug.LogWarning($"{_name} disabled GPU instancing; using pooled markers instead. {exception.Message}");
                _instancingFailureLogged = true;
             }

             return false;
          }
       }
    }

    private sealed class Timeline
    {
        private const int MajorBeatInterval = 1;
        private const float EventMarkerPositionTolerance = 0.001f;
        private readonly PluginConfig _config;
         private readonly List<TimelineLabel> _labels = [];
          private readonly List<EventMarkerDescriptor> _eventMarkerDescriptors = [];
         private readonly Dictionary<EventMarkerSpatialKey, List<int>> _eventMarkerDescriptorIndicesByCell = [];
         private readonly List<Marker> _eventMarkers = [];
         private readonly Material _lineMaterial;
         private readonly Transform _timelineRoot;
         private readonly Marker _currentMarker;
         private readonly MotionPathEventGizmoController _eventGizmoController;
        private int _majorLabelCount;
         private int _labelEndBeat = int.MinValue;
         private int _labelStartBeat = int.MinValue;
        private int _eventMarkerDescriptorCount;
        private int _firstVisibleEventMarkerIndex = -1;
         private int _visibleEventMarkerCount = -1;
         private float _beatLabelOffset = float.NaN;
         private float _beatLabelSize = float.NaN;
         private float _eventMarkerSize = float.NaN;
         private MotionPathPlan _eventMarkerPlan;
          private bool _labelsVisible;
        private bool _eventMarkerDescriptorsDirty = true;
        private bool _eventMarkersShown;
        private bool _eventMarkersEditingEnabled;
         private EventBoxGroupType? _eventIndicatorGroupTypeFilter;

        public Timeline(
           Transform parent,
            Material lineMaterial,
             Font font,
             PluginConfig config,
             MotionPathEventGizmoController eventGizmoController)
        {
            _config = config;
             _lineMaterial = lineMaterial;
             _eventGizmoController = eventGizmoController;
          var timelineObject = new GameObject("MotionPathTimeline");
          timelineObject.transform.SetParent(parent, false);
          _timelineRoot = timelineObject.transform;
           _currentMarker = new Marker(
              _timelineRoot,
              GizmoAssets.DefaultMaterial,
              "CurrentBeatMarker",
             PrimitiveType.Sphere);
          _currentMarker.Release();
          _font = font;
          Release();
       }

        private readonly Font _font;

          public bool EventEditingEnabled { get; set; }

          public void SetEventIndicatorGroupTypeFilter(EventBoxGroupType? groupType)
          {
             if (_eventIndicatorGroupTypeFilter == groupType) return;
             _eventIndicatorGroupTypeFilter = groupType;
             _eventMarkerDescriptorsDirty = true;
             InvalidateEventMarkerState();
          }

          public void SetEventMarkerPlan(MotionPathPlan plan)
          {
             if (ReferenceEquals(_eventMarkerPlan, plan)) return;
             _eventMarkerPlan = plan;
             _eventMarkerDescriptorsDirty = true;
             InvalidateEventMarkerState();
          }

        public void Release()
        {
          foreach (var marker in _eventMarkers) marker.Release();
          _currentMarker.Release();
          foreach (var label in _labels) label.Release();
          _majorLabelCount = 0;
            _labelEndBeat = int.MinValue;
            _labelStartBeat = int.MinValue;
            _beatLabelOffset = float.NaN;
            _beatLabelSize = float.NaN;
            _labelsVisible = false;
           _eventMarkerPlan = null;
           _eventMarkerDescriptorsDirty = true;
           ClearEventMarkerDescriptors();
           InvalidateEventMarkerState();
        }

        public void Update(
           RenderedTrack track,
           float minBeat,
          float maxBeat,
          float currentBeat)
      {
         if (!track.TryGetVisibleRange(minBeat, maxBeat, out var visibleMinBeat, out var visibleMaxBeat)
             || !track.TryGetPosition(currentBeat, out var currentPosition))
         {
            Release();
            return;
         }

           var labelStartBeat = Mathf.CeilToInt(visibleMinBeat);
           var labelEndBeat = Mathf.FloorToInt(visibleMaxBeat);
           var labelsVisible = _config.MotionPath.GetShowBeatLabels();
            if (labelStartBeat != _labelStartBeat
                || labelEndBeat != _labelEndBeat
                || labelsVisible != _labelsVisible
                || !Mathf.Approximately(_beatLabelSize, _config.MotionPath.GetBeatLabelSize())
                || !Mathf.Approximately(_beatLabelOffset, _config.MotionPath.GetBeatLabelOffset()))
              UpdateStaticLabels(track, labelStartBeat, labelEndBeat);

          if (_config.MotionPath.GetShowCurrentBeatMarker())
             _currentMarker.Set(
                currentPosition,
                _config.MotionPath.GetCurrentBeatMarkerSize(),
                track.IsFocused ? CurrentMarkerColor : TimelineUnfocusedColor);
          else
             _currentMarker.Release();
             UpdateEventMarkers(minBeat, maxBeat, currentBeat);
         UpdateCurrentBeatLabel(currentBeat, currentPosition);
      }

         private void UpdateEventMarkers(float minBeat, float maxBeat, float currentBeat)
         {
            _eventGizmoController.SetCurrentBeat(currentBeat);
            RebuildEventMarkerDescriptorsIfNeeded();

            var maximumMarkerCount = _config.MotionPath.GetMaximumVisibleEventMarkers();
            var showEventMarkers = _config.MotionPath.GetShowEventMarkers() && maximumMarkerCount > 0;
            var firstVisibleIndex = -1;
            var visibleMarkerCount = 0;
            if (showEventMarkers)
            {
               firstVisibleIndex = FindFirstEventMarkerAtOrAfter(minBeat);
               var firstAfterVisibleIndex = FindFirstEventMarkerAfter(maxBeat);
               visibleMarkerCount = Math.Min(firstAfterVisibleIndex - firstVisibleIndex, maximumMarkerCount);
               if (visibleMarkerCount == 0) firstVisibleIndex = -1;
            }

            var markerSize = _config.MotionPath.GetEventMarkerSize();
            if (firstVisibleIndex == _firstVisibleEventMarkerIndex
                && visibleMarkerCount == _visibleEventMarkerCount
                && showEventMarkers == _eventMarkersShown
                && Mathf.Approximately(markerSize, _eventMarkerSize)
                && EventEditingEnabled == _eventMarkersEditingEnabled)
               return;

            ApplyEventMarkerState(firstVisibleIndex, visibleMarkerCount, showEventMarkers, markerSize);
         }

         private void ApplyEventMarkerState(
            int firstVisibleIndex,
            int visibleMarkerCount,
            bool showEventMarkers,
            float markerSize)
         {
            _eventGizmoController.Clear();
            if (showEventMarkers)
            {
               EnsureMarkerCount(_eventMarkers, visibleMarkerCount, CreateEventMarker);
               _eventGizmoController.BeginMarkers();
               for (var markerIndex = 0; markerIndex < visibleMarkerCount; markerIndex++)
               {
                  var descriptor = _eventMarkerDescriptors[firstVisibleIndex + markerIndex];
                  _eventMarkers[markerIndex].Set(descriptor.Position, markerSize, EventMarkerColor);
                   _eventGizmoController.ConfigureMarker(
                      descriptor.ToEventPoint(),
                      markerSize,
                      _lineMaterial,
                      EventEditingEnabled);
               }
               ReleaseMarkersFrom(_eventMarkers, visibleMarkerCount);
               _eventGizmoController.EndMarkers();
            }
            else
               ReleaseMarkers(_eventMarkers);

            _firstVisibleEventMarkerIndex = firstVisibleIndex;
            _visibleEventMarkerCount = visibleMarkerCount;
            _eventMarkersShown = showEventMarkers;
            _eventMarkerSize = markerSize;
            _eventMarkersEditingEnabled = EventEditingEnabled;
         }

         private void RebuildEventMarkerDescriptorsIfNeeded()
         {
            if (!_eventMarkerDescriptorsDirty) return;

            ClearEventMarkerDescriptors();
            if (_eventMarkerPlan != null)
               foreach (var track in _eventMarkerPlan.Tracks)
                  if (track.EventPoints != null)
                     foreach (var point in track.EventPoints)
                        AddEventMarkerDescriptor(point);

            _eventMarkerDescriptors.Sort(0, _eventMarkerDescriptorCount, EventMarkerDescriptorComparer.Instance);
            _eventMarkerDescriptorsDirty = false;
         }

          private void AddEventMarkerDescriptor(MotionPathEventPoint point)
          {
            var descriptorIndex = FindEventMarkerDescriptor(point);
            if (descriptorIndex >= 0)
            {
               _eventMarkerDescriptors[descriptorIndex].AddSources(point.Sources, _eventIndicatorGroupTypeFilter);
               return;
            }

            while (_eventMarkerDescriptors.Count <= _eventMarkerDescriptorCount)
               _eventMarkerDescriptors.Add(new EventMarkerDescriptor());
            if (_eventMarkerDescriptors[_eventMarkerDescriptorCount].Set(
                   point,
                   _eventIndicatorGroupTypeFilter,
                   _eventMarkerDescriptorCount))
            {
               AddEventMarkerDescriptorToCell(_eventMarkerDescriptorCount, point);
               _eventMarkerDescriptorCount++;
            }
          }

          private int FindEventMarkerDescriptor(MotionPathEventPoint point)
          {
            var key = EventMarkerSpatialKey.From(point);
            var matchedDescriptorIndex = -1;
            // A tolerance-sized cell requires only adjacent cells for an exact tolerance match.
            for (var xOffset = -1; xOffset <= 1; xOffset++)
            for (var yOffset = -1; yOffset <= 1; yOffset++)
            for (var zOffset = -1; zOffset <= 1; zOffset++)
            {
               if (!_eventMarkerDescriptorIndicesByCell.TryGetValue(
                      key.Offset(xOffset, yOffset, zOffset),
                      out var descriptorIndices))
                  continue;
               foreach (var descriptorIndex in descriptorIndices)
                  if (_eventMarkerDescriptors[descriptorIndex].Matches(point)
                      && (matchedDescriptorIndex < 0
                          || _eventMarkerDescriptors[descriptorIndex].StableOrder
                          < _eventMarkerDescriptors[matchedDescriptorIndex].StableOrder))
                     matchedDescriptorIndex = descriptorIndex;
            }
            return matchedDescriptorIndex;
         }

         private void AddEventMarkerDescriptorToCell(int descriptorIndex, MotionPathEventPoint point)
         {
            var key = EventMarkerSpatialKey.From(point);
            if (!_eventMarkerDescriptorIndicesByCell.TryGetValue(key, out var descriptorIndices))
            {
               descriptorIndices = [];
               _eventMarkerDescriptorIndicesByCell.Add(key, descriptorIndices);
            }
            descriptorIndices.Add(descriptorIndex);
         }

         private int FindFirstEventMarkerAtOrAfter(float beat) => FindFirstEventMarker(beat, false);

         private int FindFirstEventMarkerAfter(float beat) => FindFirstEventMarker(beat, true);

         private int FindFirstEventMarker(float beat, bool strictlyAfter)
         {
            var low = 0;
            var high = _eventMarkerDescriptorCount;
            while (low < high)
            {
               var middle = low + (high - low) / 2;
               var markerBeat = _eventMarkerDescriptors[middle].Beat;
               if (markerBeat < beat || strictlyAfter && markerBeat == beat) low = middle + 1;
               else high = middle;
            }
            return low;
         }

          private void ClearEventMarkerDescriptors()
          {
            for (var descriptorIndex = 0; descriptorIndex < _eventMarkerDescriptorCount; descriptorIndex++)
               _eventMarkerDescriptors[descriptorIndex].Clear();
            _eventMarkerDescriptorIndicesByCell.Clear();
            _eventMarkerDescriptorCount = 0;
          }

         private void InvalidateEventMarkerState()
         {
            ReleaseMarkers(_eventMarkers);
            _eventGizmoController.Clear();
            _firstVisibleEventMarkerIndex = -1;
            _visibleEventMarkerCount = -1;
            _eventMarkersShown = false;
            _eventMarkerSize = float.NaN;
            _eventMarkersEditingEnabled = false;
         }

       private void UpdateStaticLabels(RenderedTrack track, int labelStartBeat, int labelEndBeat)
       {
            var labelCount = 0;
            _labelStartBeat = labelStartBeat;
            _labelEndBeat = labelEndBeat;
            _labelsVisible = _config.MotionPath.GetShowBeatLabels();
            _beatLabelOffset = _config.MotionPath.GetBeatLabelOffset();
            _beatLabelSize = _config.MotionPath.GetBeatLabelSize();
           if (_labelsVisible)
           {
              var firstMajorBeat = Mathf.CeilToInt(labelStartBeat / (float)MajorBeatInterval) * MajorBeatInterval;
              for (var beat = firstMajorBeat; beat <= labelEndBeat; beat += MajorBeatInterval)
             {
                track.TryGetPosition(beat, out var position);
                EnsureLabelCount(labelCount + 1);
                _labels[labelCount++].Set(
                   position + Vector3.up * _config.MotionPath.GetBeatLabelOffset(),
                   beat.ToString(CultureInfo.InvariantCulture),
                   TimelineBeatColor,
                   _config.MotionPath.GetBeatLabelSize());
             }
          }
          _majorLabelCount = labelCount;
          ReleaseLabelsFrom(labelCount);
      }

      private void UpdateCurrentBeatLabel(float currentBeat, Vector3 currentPosition)
      {
          if (!_config.MotionPath.GetShowBeatLabels())
          {
             ReleaseLabelsFrom(_majorLabelCount);
             return;
          }

          if (IsMajorBeat(currentBeat))
          {
             ReleaseLabelsFrom(_majorLabelCount);
            return;
          }

          EnsureLabelCount(_majorLabelCount + 1);
          _labels[_majorLabelCount].Set(
             currentPosition + Vector3.up * _config.MotionPath.GetBeatLabelOffset(),
             currentBeat.ToString("0.##", CultureInfo.InvariantCulture),
             CurrentMarkerColor,
             _config.MotionPath.GetBeatLabelSize());
          ReleaseLabelsFrom(_majorLabelCount + 1);
      }

          private Marker CreateEventMarker() =>
            new(
               _timelineRoot,
               GizmoAssets.DefaultMaterial,
               "EventMarker",
             PrimitiveType.Cube,
             Quaternion.Euler(45f, 45f, 45f));

       private void EnsureLabelCount(int count)
       {
          while (_labels.Count < count) _labels.Add(new TimelineLabel(_timelineRoot, _font));
       }

       private static void EnsureMarkerCount(List<Marker> markers, int count, Func<Marker> createMarker)
       {
          while (markers.Count < count) markers.Add(createMarker());
       }

       private static void ReleaseMarkers(List<Marker> markers) => ReleaseMarkersFrom(markers, 0);

       private static void ReleaseMarkersFrom(List<Marker> markers, int firstUnusedIndex)
       {
          for (var markerIndex = firstUnusedIndex; markerIndex < markers.Count; markerIndex++)
             markers[markerIndex].Release();
       }

       private void ReleaseLabelsFrom(int firstUnusedIndex)
       {
          for (var labelIndex = firstUnusedIndex; labelIndex < _labels.Count; labelIndex++)
             _labels[labelIndex].Release();
       }

        private static bool IsMajorBeat(float beat)
        {
           return Mathf.Abs(beat - Mathf.Round(beat / MajorBeatInterval) * MajorBeatInterval) < 0.001f;
        }

        private readonly struct EventMarkerSpatialKey : IEquatable<EventMarkerSpatialKey>
        {
           private EventMarkerSpatialKey(float beat, int x, int y, int z)
           {
              Beat = beat;
              X = x;
              Y = y;
              Z = z;
           }

           private float Beat { get; }
           private int X { get; }
           private int Y { get; }
           private int Z { get; }

           public static EventMarkerSpatialKey From(MotionPathEventPoint point) => new(
              point.Beat,
              GetCellCoordinate(point.Position.x),
              GetCellCoordinate(point.Position.y),
              GetCellCoordinate(point.Position.z));

           public EventMarkerSpatialKey Offset(int x, int y, int z) =>
              new(Beat, X + x, Y + y, Z + z);

           public bool Equals(EventMarkerSpatialKey other) =>
              Beat.Equals(other.Beat) && X == other.X && Y == other.Y && Z == other.Z;

           public override bool Equals(object obj) => obj is EventMarkerSpatialKey other && Equals(other);

           public override int GetHashCode()
           {
              unchecked
              {
                 var hash = Beat.GetHashCode();
                 hash = hash * 31 + X;
                 hash = hash * 31 + Y;
                 return hash * 31 + Z;
              }
           }

           private static int GetCellCoordinate(float coordinate) =>
              Mathf.FloorToInt(coordinate / EventMarkerPositionTolerance);
        }

         private sealed class EventMarkerDescriptor
        {
          private readonly List<MotionPathEventSource> _sources = [];

           private float _beat;
           private Vector3 _position;
           private int _stableOrder;

           public float Beat => _beat;
           public Vector3 Position => _position;
           public int StableOrder => _stableOrder;

          public void AddSources(
             IReadOnlyList<MotionPathEventSource> sources,
             EventBoxGroupType? groupTypeFilter)
          {
            if (sources == null) return;
            foreach (var source in sources)
            {
               if (groupTypeFilter.HasValue && source.GroupType != groupTypeFilter.Value) continue;
               var alreadyAdded = false;
                foreach (var current in _sources)
                   if (IsSameSource(current, source))
                    {
                       alreadyAdded = true;
                       break;
                    }
                if (!alreadyAdded) _sources.Add(source);
             }
          }

           public bool Matches(MotionPathEventPoint point) =>
              _beat == point.Beat
              && (_position - point.Position).sqrMagnitude
              <= EventMarkerPositionTolerance * EventMarkerPositionTolerance;

            public bool Set(
               MotionPathEventPoint point,
               EventBoxGroupType? groupTypeFilter,
               int stableOrder)
            {
               _beat = point.Beat;
               _position = point.Position;
               _stableOrder = stableOrder;
               _sources.Clear();
               AddSources(point.Sources, groupTypeFilter);
               return _sources.Count > 0;
            }

           public MotionPathEventPoint ToEventPoint() => new(_beat, _position, _sources);

           public void Clear() => _sources.Clear();

           private static bool IsSameSource(MotionPathEventSource left, MotionPathEventSource right) =>
              ReferenceEquals(left.RuntimeSourceIdentity, right.RuntimeSourceIdentity);
        }

        private sealed class EventMarkerDescriptorComparer : IComparer<EventMarkerDescriptor>
        {
           public static readonly EventMarkerDescriptorComparer Instance = new();

           public int Compare(EventMarkerDescriptor left, EventMarkerDescriptor right)
           {
              var beatComparison = left.Beat.CompareTo(right.Beat);
              return beatComparison != 0 ? beatComparison : left.StableOrder.CompareTo(right.StableOrder);
           }
        }
    }

   private sealed class Marker
   {
      private readonly MaterialPropertyBlock _properties = new();
      private readonly MeshRenderer _renderer;

       public Marker(
          Transform parent,
          Material material,
          string name,
          PrimitiveType primitiveType,
          Quaternion? rotation = null)
          : this(
             UnityEngine.GameObject.CreatePrimitive(primitiveType),
             parent,
             material,
             name,
             null,
             rotation ?? Quaternion.identity)
       {
       }

       public Marker(Transform parent, Material material, string name, Mesh sharedMesh)
          : this(new GameObject(name), parent, material, name, sharedMesh, Quaternion.identity)
       {
       }

       private Marker(
          GameObject gameObject,
          Transform parent,
          Material material,
          string name,
          Mesh sharedMesh,
          Quaternion rotation)
       {
          GameObject = gameObject;
          GameObject.name = name;
          GameObject.transform.SetParent(parent, false);
          GameObject.transform.localRotation = rotation;
          if (sharedMesh != null) GameObject.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
          _renderer = GameObject.GetComponent<MeshRenderer>() ?? GameObject.AddComponent<MeshRenderer>();
          _renderer.sharedMaterial = material;
          var collider = GameObject.GetComponent<Collider>();
          if (collider != null) Object.Destroy(collider);
      }

      public GameObject GameObject { get; }

      public void Release() => GameObject.SetActive(false);

       public void Set(Vector3 position, float size, Color color, Quaternion? rotation = null)
       {
          GameObject.SetActive(true);
          GameObject.transform.position = position;
          GameObject.transform.localScale = Vector3.one * size;
          if (rotation.HasValue) GameObject.transform.rotation = rotation.Value;
          SetColor(color);
       }

      public void SetColor(Color color)
      {
         _properties.SetColor("_Color", color);
         _renderer.SetPropertyBlock(_properties);
      }
   }

   private sealed class TimelineLabel
   {
      private readonly TextMesh _text;

      public TimelineLabel(Transform parent, Font font)
      {
         GameObject = new GameObject("BeatLabel");
         GameObject.transform.SetParent(parent, false);
         GameObject.AddComponent<MotionPathBillboard>();
         _text = GameObject.AddComponent<TextMesh>();
          _text.anchor = TextAnchor.MiddleCenter;
          _text.alignment = TextAlignment.Center;
          _text.fontSize = 40;
         _text.font = font;
         if (font != null) GameObject.GetComponent<MeshRenderer>().sharedMaterial = font.material;
      }

      public GameObject GameObject { get; }

      public void Release() => GameObject.SetActive(false);

       public void Set(Vector3 position, string value, Color color, float size)
       {
          GameObject.SetActive(true);
          GameObject.transform.position = position;
          _text.text = value;
          _text.color = color;
          _text.characterSize = size;
       }
   }
}
