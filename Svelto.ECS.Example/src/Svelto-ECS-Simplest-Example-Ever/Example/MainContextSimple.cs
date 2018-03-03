//#define PROFILE

using System;
using System.Collections;
using System.Threading;
using Svelto.ECS.Schedulers;
using Svelto.ECS.Vanilla.Example.SimpleEntityAsClass.SimpleEntity;
using Svelto.ECS.Vanilla.Example.SimpleEntityAsClass.SimpleEntityEngine;
using Svelto.ECS.Vanilla.Example.SimpleEntityAsStruct.SimpleEntityStruct;
using Svelto.ECS.Vanilla.Example.SimpleEntityAsStruct.SimpleEntityStructEngine;
using Svelto.WeakEvents;
using Console = Utility.Console;

#region comment

///
/// Promoting a strict hierarchical namespace structure is an important part of the Svelto.ECS
/// Philosophy. EntityViews must be used by Engines belonging to the same namespace or parent
/// one
///  

#endregion

#if PROFILE
using System.Diagnostics;
#endif

namespace Svelto.ECS.Vanilla.Example
{
    public class Program
    {
        static SimpleContext simpleContext;

        static void Main(string[] args)
        {
            simpleContext = new SimpleContext();

            while (true) Thread.Sleep(1000);
        }
    }

    #region comment

    /// <summary>
    ///     The Context is the framework starting point.
    ///     As Composition root, it gives to the coder the responsibility to create,
    ///     initialize and inject dependencies.
    ///     Every application can have more than one context and every context can have one
    ///     or more composition roots (a facade, but even a factory, can be a composition root)
    /// </summary>

    #endregion
    public class SimpleContext
    {
        EnginesRoot _enginesRoot;

        #region comment

        /// <summary>
        ///     Naivily we run the mainloop inside the constructor using Svelto.Tasks
        ///     extension. MainApplicationLoop() is the simple extension to run whatever IEnumerator.
        /// </summary>

        #endregion
        public SimpleContext()
        {
            MainApplicationLoop().Run();
        }

        IEnumerator MainApplicationLoop()
        {
            #region comment  

            //An EnginesRoot holds all the engines created. 
            //it needs a EntitySubmissionScheduler to know when to
            //add the EntityViews generated inside the EntityDB.
            #endregion

            var simpleSubmissionEntityViewScheduler = new SimpleSubmissionEntityViewScheduler();
            _enginesRoot = new EnginesRoot(simpleSubmissionEntityViewScheduler);

            #region comment

            //an EnginesRoot must never be injected inside other classes
            //only IEntityFactory and IEntityFunctions implementation can

            #endregion

            var entityFactory   = _enginesRoot.GenerateEntityFactory();
            var entityFunctions = _enginesRoot.GenerateEntityFunctions();

            //Add the Engine to manage the SimpleEntities
            _enginesRoot.AddEngine(new BehaviourForSimpleEntityEngine(entityFunctions));
            //Add the Engine to manage the SimpleStructEntities
            _enginesRoot.AddEngine(new BehaviourForSimpleEntityAsStructEngine());

            #region comment

            //the number of implementors to use to implement the Entity components is arbitrary and it depends
            //by the modularity/reusability of the implementors.
            //You may avoid to create new implementors if you create modular/reusable ones
            //The concept of implementor is pretty unique in Svelto.ECS
            //and enable very interesting features. Refer to my articles to understand
            //more about it.

            #endregion

            #region comment
            //every entity component must be implemented with its own implementor obviously
            //(otherwise the instances will be shared between entities and
            //we don't want that, right? :) )
            #endregion
            
            #region comment
            //Build a SimpleEntity using specific implementors to implement the 
            //Entity Components interfaces. The number of implementors can vary
            //but for the purposes of this example, one is enough.
            #endregion
            entityFactory.BuildEntity<SimpleEntityDescriptor>(1, new[] {new SimpleImplementor("simpleEntity")});

#region comment           

            //Entities as struct do not need an implementor. They are much more rigid
            //to use, but much faster. Please use them only when performance is really
            //critical (most of the time is not)
            //Build a SimpleStructEntity inside the group groupID
            //Grouped entities can then queried by group. In a more complex scenario
            //it would be more used than not.
            //Build and BuildEntityInGroup can be used either with EntityView and EntityStructs
            #endregion
            entityFactory.BuildEntityInGroup<SimpleStructEntityDescriptor>(2, 0, null);

            entityFactory.BuildEntityInGroup<SimpleGroupedEntityDescriptor>(0, 0, new[] {new SimpleImplementor("simpleGroupedEntity")});

            #region comment

            //quick way to submit entities, this is not the standard way, but if you
            //create a custom EntitySubmissionScheduler is up to you to decide
            //when the EntityViews are submited to the engines and DB.
            //In Unity this step is hidden by the UnitySumbmissionEntityViewScheduler
            //logic

            #endregion

            simpleSubmissionEntityViewScheduler.SubmitEntities();

            Console.Log("Done - click any button to quit");

            System.Console.ReadKey();

            Environment.Exit(0);

            yield break;
        }
   }

    namespace SimpleEntityAsClass
    {
        namespace SimpleEntity
        {
            //just a custom component as proof of concept
            public interface ISimpleComponent
            {
                string name    { get; }
            }

            //the implementor(s) implement the components of the Entity. In Svelto.ECS
            //components are always interfaces when Entities as classes are used
            class SimpleImplementor : ISimpleComponent
            {
                public SimpleImplementor(string name)
                {
                    this.name = name;
                }

                public string name { get; }
            }

            #region comment

            //The EntityDescriptor identify your Entity. It's essential to identify
            //your entities with a name that comes from the Game Design domain.
            //More about this on my articles.

            #endregion

            class SimpleEntityDescriptor : GenericEntityDescriptor<BehaviourEntityViewForSimpleEntity>
            {
            }

            class SimpleGroupedEntityDescriptor : GenericEntityDescriptor<BehaviourEntityViewForSimpleGroupedEntity>
            {
            }
        }

        namespace SimpleEntityEngine
        {
            #region comment

            /// <summary>
            ///     In order to show as many features as possible, I created this pretty useless Engine that
            ///     accepts two Entities (so I can show the use of a MultiEntitiesViewEngine).
            ///     Now, keep this in mind: an Engine should seldomly inherit from SingleEntityViewEngine
            ///     or MultiEngineEntityViewsEngine.
            ///     The Add and Remove callbacks per EntityView submitted is another unique feature of Svelto.ECS
            ///     and they are meant to be used if STRICTLY necessary. The feature has been mainly added
            ///     to setup DispatchOnChange and DispatchOnSet (check my articles to know more), but
            ///     it can be exploited for other reasons if well thought through!
            ///     So, yes, normally engines just implement IQueryingEntityViewEngine
            /// </summary>

            #endregion
            public class BehaviourForSimpleEntityEngine : MultiEntityViewsEngine<BehaviourEntityViewForSimpleEntity,
                BehaviourEntityViewForSimpleGroupedEntity>
            {
                readonly IEntityFunctions _entityFunctions;

                public BehaviourForSimpleEntityEngine(IEntityFunctions entityFunctions)
                {
                    _entityFunctions = entityFunctions;
                }

                protected override void Add(BehaviourEntityViewForSimpleEntity entity)
                {
#if !PROFILE
                    Console.Log("EntityView Added");

                    _entityFunctions.RemoveEntity(entity.ID);
#endif
                }

                protected override void Remove(BehaviourEntityViewForSimpleEntity entity)
                {
                    Console.Log(entity.simpleComponent.name + "EntityView Removed");
                }

                /// <summary>
                ///     With the following code I demostrate two features:
                ///     First how to move an entity between groups
                ///     Second how to remove an entity from a group
                /// </summary>
                /// <param name="entity"></param>
                protected override void Add(BehaviourEntityViewForSimpleGroupedEntity entity)
                {
                    Console.Log("Grouped EntityView Added");

                    _entityFunctions.SwapEntityGroup(entity.ID, 0, 1);
                    Console.Log("Grouped EntityView Swapped");
                    _entityFunctions.RemoveEntity(entity.ID);
                }

                protected override void Remove(BehaviourEntityViewForSimpleGroupedEntity entity)
                {
                    Console.Log("Grouped EntityView Removed");
                }
            }

            #region comment

            /// <summary>
            ///     You must always design an Engine according the Entities it must handle.
            ///     The Entities must be real application/game entities and cannot be
            ///     abstract concepts (It would be very dangerous to create abstract/meaningless
            ///     entities just for the purpose to write specific logic).
            ///     An EntityView is just how an Engine see a specific Entity, this allows
            ///     filtering of the components and promote abstraction/encapsulation
            /// </summary>

            #endregion
            public class BehaviourEntityViewForSimpleEntity : EntityView
            {
                public ISimpleComponent simpleComponent;
            }

            public class BehaviourEntityViewForSimpleGroupedEntity : EntityView
            {
                public ISimpleComponent simpleComponent;
            }
        }
    }

    namespace SimpleEntityAsStruct
    {
        //Let's not get things more complicated than they really are, in fact it's pretty
        //simple, an Entity can be made of Struct Components and/or Interface components
        //When you want build an entity entirely or partially upon struct components
        //you inherit from a MixedEntityDescriptor. In this case, you must explicitly
        //define which EntityView builder you want to use.
        //An EntityViewStructBuilder will create a SimpleEntityViewStruct
        namespace SimpleEntityStruct
        {
            class SimpleStructEntityDescriptor : MixedEntityDescriptor
                <EntityViewStructBuilder<BehaviourEntityViewForSimpleStructEntity>>
            {
            }
        }

        namespace SimpleEntityStructEngine
        {
            //An EntityViewStruct must always implement the IEntityStruct interface
            //don't worry, boxing/unboxing will never happen.
            struct BehaviourEntityViewForSimpleStructEntity : IEntityStruct
            {
                public int ID { get; set; }

                public int counter;
            }

            /// <summary>
            ///     SingleEntityViewEngine and MultiEntityViewsEngine cannot be used with
            ///     EntityView as struct as it would not make any sense. EntityViews as
            ///     struct are meant to be use for tight high performance loops where
            ///     cache coherence is considered during the design process
            /// </summary>
            public class BehaviourForSimpleEntityAsStructEngine : IQueryingEntityViewEngine
            {
                public IEntityViewsDB entityViewsDB { private get; set; }

                public void Ready()
                {
                    Update().Run();
                }

                IEnumerator Update()
                {
                    Console.Log("Task Waiting");

                    while (true)
                    {
                        var entityViews =
                            entityViewsDB
                               .QueryGroupedEntityViewsAsArray<BehaviourEntityViewForSimpleStructEntity
                                >(0, out var count);

                        if (count > 0)
                        {
                            for (var i = 0; i < count; i++)
                                AddOne(ref entityViews[i].counter);

                            Console.Log("Task Done");

                            yield break;
                        }

                        yield return null;
                    }
                }

                static void AddOne(ref int counter)
                {
                    counter += 1;
                }
            }
        }
    }
}