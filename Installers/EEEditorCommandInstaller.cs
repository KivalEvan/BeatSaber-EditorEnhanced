using BeatmapEditor3D;
using EditorEnhanced.Commands;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEEditorCommandInstaller
    : Installer
{
    public override void InstallBindings()
    {
        var commandContainer = Container.Resolve<DiContainer>();
        InstallCommands<SortAxisEventBoxGroupSignal, SortAxisEventBoxGroupCommand>(Container, commandContainer);
        InstallCommands<SortIdEventBoxGroupSignal, SortIdEventBoxGroupCommand>(Container, commandContainer);
        InstallCommands<CopyEventBoxSignal, CopyEventBoxCommand>(Container, commandContainer);
        InstallCommands<PasteEventBoxSignal, PasteEventBoxCommand>(Container, commandContainer);
        InstallCommands<DuplicateEventBoxSignal, DuplicateEventBoxCommand>(Container, commandContainer);
        InstallCommands<LolighterSignal, LolighterCommand>(Container, commandContainer);

        Container.DeclareSignal<EventBoxSelectedSignal>().OptionalSubscriber();
        Container.DeclareSignal<GizmoUpdateSignal>().OptionalSubscriber();
    }

    private static void InstallCommands<TSignal, TCommand>(DiContainer container, DiContainer commandContainer)
        where TCommand : IBeatmapEditorCommand
    {
        commandContainer.BindFactory<TCommand, PlaceholderFactory<TCommand>>();
        container.DeclareSignal<TSignal>().OptionalSubscriber();
        container.BindSignal<TSignal>()
            .ToMethod<BeatmapEditorCommandRunnerSignalBinder>(binder =>
                binder.BindSignal<TSignal, TCommand>).FromResolve();
    }
}