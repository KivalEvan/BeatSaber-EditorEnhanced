using BeatmapEditor3D;
using EditorEnhanced.Commands;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEEditorCommandInitializer : IInitializable
{
    private readonly BeatmapEditorCommandRunnerSignalBinder _signalBinder;

    public EEEditorCommandInitializer(BeatmapEditorCommandRunnerSignalBinder signalBinder)
    {
        _signalBinder = signalBinder;
    }

    public void Initialize()
    {
        _signalBinder._commandContainer
            .BindFactory<DragGizmoLightTranslationEventBoxCommand,
                PlaceholderFactory<DragGizmoLightTranslationEventBoxCommand>>();
        _signalBinder._commandContainer
            .BindFactory<SortAxisEventBoxGroupCommand, PlaceholderFactory<SortAxisEventBoxGroupCommand>>();
        _signalBinder._commandContainer
            .BindFactory<SortIdEventBoxGroupCommand, PlaceholderFactory<SortIdEventBoxGroupCommand>>();
        _signalBinder._commandContainer
            .BindFactory<ReorderEventBoxCommand, PlaceholderFactory<ReorderEventBoxCommand>>();
        _signalBinder._commandContainer
            .BindFactory<CopyEventBoxCommand, PlaceholderFactory<CopyEventBoxCommand>>();
        _signalBinder._commandContainer
            .BindFactory<PasteEventBoxCommand, PlaceholderFactory<PasteEventBoxCommand>>();
        _signalBinder._commandContainer
            .BindFactory<DuplicateEventBoxCommand, PlaceholderFactory<DuplicateEventBoxCommand>>();
        _signalBinder._commandContainer
            .BindFactory<LolighterCommand, PlaceholderFactory<LolighterCommand>>();
    }
}