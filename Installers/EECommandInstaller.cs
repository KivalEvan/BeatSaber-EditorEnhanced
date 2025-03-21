using BeatmapEditor3D;
using EditorEnhanced.Commands;
using Zenject;

namespace EditorEnhanced.Installers;

public class EECommandInstaller
    : Installer
{
    public override void InstallBindings()
    {
        // this is the dumbest way i ever had to deal with DI
        Container.BindInterfacesAndSelfTo<EECommandInitializer>().AsSingle();
        InstallCommands<ReorderEventBoxSignal, ReorderEventBoxCommand>(Container);
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