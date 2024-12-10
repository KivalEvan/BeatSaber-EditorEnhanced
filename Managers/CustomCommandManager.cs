using System;
using BeatmapEditor3D;
using Zenject;

namespace EditorEnhanced.Commands;

public class CustomCommandManager(BeatmapEditorCommandRunnerSignalBinder signalBinder)
    : IInitializable
{
    public void Initialize()
    {
        signalBinder._commandContainer
            .BindFactory<ReorderEventBoxCommand, PlaceholderFactory<ReorderEventBoxCommand>>();
        signalBinder._commandContainer
            .BindFactory<LolighterCommand, PlaceholderFactory<LolighterCommand>>();
    }
}