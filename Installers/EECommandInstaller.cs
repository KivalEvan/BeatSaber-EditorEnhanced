using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.EventBoxes;
using EditorEnhanced.Gizmo.Commands;
using Zenject;

namespace EditorEnhanced.Installers;

public class EECommandInstaller : Installer
{
   public override void InstallBindings()
   {
      Container.Bind<EventBoxGroupMutation>().AsSingle();
      Container.Bind<EventBoxCloneService>().AsSingle();
      Container.BindInterfacesTo<CommandFactoryInitializer>().AsSingle();

      InstallCommands<SortAxisEventBoxGroupSignal, SortAxisEventBoxGroupCommand>();
      InstallCommands<SortIdEventBoxGroupSignal, SortIdEventBoxGroupCommand>();
      InstallCommands<DragGizmoLightTranslationEventBoxSignal, DragGizmoLightTranslationEventBoxCommand>();
      InstallCommands<DragGizmoLightRotationEventBoxSignal, DragGizmoLightRotationEventBoxCommand>();
      InstallCommands<ScrollGizmoEventBoxLaneSignal, ScrollGizmoEventBoxLaneCommand>();
      InstallCommands<ReorderEventBoxSignal, ReorderEventBoxCommand>();
      InstallCommands<CopyEventBoxSignal, CopyEventBoxCommand>();
      InstallCommands<PasteEventBoxSignal, PasteEventBoxCommand>();
      InstallCommands<DuplicateEventBoxSignal, DuplicateEventBoxCommand>();

      // Gizmo Listener
      Container.DeclareSignal<EventBoxSelectedSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoRefreshSignal>().OptionalSubscriber();

      // Gizmo Config
      Container.DeclareSignal<GizmoColliderConfigChangedSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoScaleConfigChangedSignal>().OptionalSubscriber();
   }

   private void InstallCommands<TSignal, TCommand>()
      where TCommand : IBeatmapEditorCommand
   {
      Container.DeclareSignal<TSignal>().OptionalSubscriber();
      Container
         .BindSignal<TSignal>()
         .ToMethod<BeatmapEditorCommandRunnerSignalBinder>(binder =>
            binder.BindSignal<TSignal, TCommand>)
         .FromResolve();
   }
}

public class CommandFactoryInitializer : IInitializable
{
   [Inject(Id = "SignalsContainer")] private readonly DiContainer _commandContainer = null!;

   public void Initialize()
   {
      BindFactory<SortAxisEventBoxGroupCommand>();
      BindFactory<SortIdEventBoxGroupCommand>();
      BindFactory<DragGizmoLightTranslationEventBoxCommand>();
      BindFactory<DragGizmoLightRotationEventBoxCommand>();
      BindFactory<ScrollGizmoEventBoxLaneCommand>();
      BindFactory<ReorderEventBoxCommand>();
      BindFactory<CopyEventBoxCommand>();
      BindFactory<PasteEventBoxCommand>();
      BindFactory<DuplicateEventBoxCommand>();
   }

   private void BindFactory<TCommand>() where TCommand : IBeatmapEditorCommand
   {
      _commandContainer.BindFactory<TCommand, PlaceholderFactory<TCommand>>();
   }
}
