using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.Patches;
using Zenject;

namespace EditorEnhanced.Installers;

public class EEEditorCommandInstaller
    : Installer
{
    public override void InstallBindings()
    {
        // this is the dumbest way i ever had to deal with DI
        Container.BindInterfacesAndSelfTo<EEEditorCommandInitializer>().AsSingle();
        InstallCommands<ReorderEventBoxSignal, ReorderEventBoxCommand>(Container);
        InstallCommands<CopyEventBoxSignal, CopyEventBoxCommand>(Container);
        InstallCommands<PasteEventBoxSignal, PasteEventBoxCommand>(Container);
        InstallCommands<PasteEventBoxSeedSignal, PasteEventBoxSeedCommand>(Container);
        InstallCommands<DuplicateEventBoxSignal, DuplicateEventBoxCommand>(Container);
        InstallCommands<LolighterSignal, LolighterCommand>(Container);
    }

    private static void InstallCommands<TSignal, TCommand>(DiContainer container) where TCommand : IBeatmapEditorCommand
    {
        container.DeclareSignal<TSignal>().OptionalSubscriber();
        container.BindSignal<TSignal>()
            .ToMethod<BeatmapEditorCommandRunnerSignalBinder>(binder =>
                binder.BindSignal<TSignal, TCommand>).FromResolve();
    }
}