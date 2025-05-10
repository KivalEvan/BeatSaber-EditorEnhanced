using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.Patches;
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
        InstallCommands<CopyEventBoxSignal, CopyEventBoxCommand>(Container);
        InstallCommands<PasteEventBoxSignal, PasteEventBoxCommand>(Container);
        InstallCommands<DuplicateEventBoxSignal, DuplicateEventBoxCommand>(Container);
        InstallCommands<LolighterSignal, LolighterCommand>(Container);

        Container.BindInterfacesAndSelfTo<ModifyHoveredLightEventDeltaIntensityCommandPatches>().AsSingle();
    }

    private static void InstallCommands<TSignal, TCommand>(DiContainer container) where TCommand : IBeatmapEditorCommand
    {
        container.DeclareSignal<TSignal>().OptionalSubscriber();
        container.BindSignal<TSignal>()
            .ToMethod<BeatmapEditorCommandRunnerSignalBinder>(binder =>
                binder.BindSignal<TSignal, TCommand>).FromResolve();
    }
}