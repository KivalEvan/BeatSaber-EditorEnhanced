using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.Gizmo.Commands;
using Zenject;

namespace EditorEnhanced.Installers;

public class EECommandInstaller : Installer
{
   public override void InstallBindings()
   {
      Container.BindInterfacesTo<StupidCommandInitializer>().AsSingle();

      InstallCommands<SortAxisEventBoxGroupSignal, SortAxisEventBoxGroupCommand>();
      InstallCommands<SortIdEventBoxGroupSignal, SortIdEventBoxGroupCommand>();
      InstallCommands<DragGizmoLightTranslationEventBoxSignal, DragGizmoLightTranslationEventBoxCommand>();
      InstallCommands<DragGizmoLightRotationEventBoxSignal, DragGizmoLightRotationEventBoxCommand>();
      InstallCommands<ReorderEventBoxSignal, ReorderEventBoxCommand>();
      InstallCommands<CopyEventBoxSignal, CopyEventBoxCommand>();
      InstallCommands<PasteEventBoxSignal, PasteEventBoxCommand>();
      InstallCommands<DuplicateEventBoxSignal, DuplicateEventBoxCommand>();
      InstallCommands<LolighterSignal, LolighterCommand>();

      // Gizmo Listener
      Container.DeclareSignal<GizmoEventBoxSelectedSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoRefreshSignal>().OptionalSubscriber();

      // Gizmo Config
      Container.DeclareSignal<GizmoConfigDraggableUpdateSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoConfigSwappableUpdateSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoConfigHighlightUpdateSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoConfigRaycastGizmoUpdateSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoConfigRaycastLaneUpdateSignal>().OptionalSubscriber();

      Container.DeclareSignal<GizmoConfigIdColorUpdateSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoConfigDistributeShapeUpdateSignal>().OptionalSubscriber();

      Container.DeclareSignal<GizmoConfigShowBaseUpdateSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoConfigShowModifierUpdateSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoConfigShowLaneUpdateSignal>().OptionalSubscriber();

      Container.DeclareSignal<GizmoConfigGlobalScaleUpdateSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoConfigSizeBaseUpdateSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoConfigSizeRotationUpdateSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoConfigSizeTranslationUpdateSignal>().OptionalSubscriber();

      Container.DeclareSignal<GizmoConfigDefaultColorUpdateSignal>().OptionalSubscriber();
      Container.DeclareSignal<GizmoConfigHighlightColorUpdateSignal>().OptionalSubscriber();
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

public class StupidCommandInitializer : IInitializable
{
   [Inject(Id = "SignalsContainer")] private readonly DiContainer _commandContainer = null!;

   public void Initialize()
   {
      BindThatFact<SortAxisEventBoxGroupCommand>();
      BindThatFact<SortIdEventBoxGroupCommand>();
      BindThatFact<DragGizmoLightTranslationEventBoxCommand>();
      BindThatFact<DragGizmoLightRotationEventBoxCommand>();
      BindThatFact<ReorderEventBoxCommand>();
      BindThatFact<CopyEventBoxCommand>();
      BindThatFact<PasteEventBoxCommand>();
      BindThatFact<DuplicateEventBoxCommand>();
      BindThatFact<LolighterCommand>();
   }

   private void BindThatFact<TCommand>() where TCommand : IBeatmapEditorCommand
   {
      _commandContainer.BindFactory<TCommand, PlaceholderFactory<TCommand>>();
   }
}