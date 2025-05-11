using BeatmapEditor3D;
using EditorEnhanced.Commands;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEEditorCommandInitializer(BeatmapEditorCommandRunnerSignalBinder signalBinder)
    : IInitializable
{
    public void Initialize()
    {
        signalBinder._commandContainer
            .BindFactory<ReorderEventBoxCommand, PlaceholderFactory<ReorderEventBoxCommand>>();
        signalBinder._commandContainer
            .BindFactory<CopyEventBoxCommand, PlaceholderFactory<CopyEventBoxCommand>>();
        signalBinder._commandContainer
            .BindFactory<PasteEventBoxCommand, PlaceholderFactory<PasteEventBoxCommand>>();
        signalBinder._commandContainer
            .BindFactory<PasteEventBoxSeedCommand, PlaceholderFactory<PasteEventBoxSeedCommand>>();
        signalBinder._commandContainer
            .BindFactory<DuplicateEventBoxCommand, PlaceholderFactory<DuplicateEventBoxCommand>>();
        signalBinder._commandContainer
            .BindFactory<LolighterCommand, PlaceholderFactory<LolighterCommand>>();
    }
}