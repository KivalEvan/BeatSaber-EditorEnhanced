using BeatmapEditor3D;
using EditorEnhanced.Commands;
using Zenject;

namespace EditorEnhanced.Installers;

public class EECommandInitializer(BeatmapEditorCommandRunnerSignalBinder signalBinder)
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