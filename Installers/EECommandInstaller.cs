using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
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
        Container.BindInterfacesAndSelfTo<CustomCommandManager>().AsSingle();
        InstallCommands<ReorderEventBoxSignal, ReorderEventBoxCommand>(Container);
    }

    private static void InstallCommands<TSignal, TCommand>(DiContainer container) where TCommand : IBeatmapEditorCommand
    {
        container.DeclareSignal<TSignal>().OptionalSubscriber();
        container.BindSignal<TSignal>()
            .ToMethod<BeatmapEditorCommandRunnerSignalBinder>(binder =>
                binder.BindSignal<TSignal, TCommand>).FromResolve();
    }
}