//#define PROFILE

using System;
using System.Collections;
using System.Threading;
using Svelto.ECS.Schedulers;
#region comment
///
/// Promoting a strict hierarchical namespace structure is an important part of the Svelto.ECS
/// Philosophy. EntityViews must be used by Engines belonging to the same namespace or parent
/// one
///  
#endregion
using Svelto.ECS.Vanilla.Example.SimpleEntityAsClass.SimpleEntity;
using Svelto.ECS.Vanilla.Example.SimpleEntityAsClass.SimpleEntityEngine;
using Svelto.ECS.Vanilla.Example.SimpleEntityAsStruct.SimpleEntityStruct;
using Svelto.ECS.Vanilla.Example.SimpleEntityAsStruct.SimpleEntityStructEngine;
using Svelto.WeakEvents;

#if PROFILE
using System.Diagnostics;
#endif

namespace Svelto.ECS.Vanilla.Example
{
    #region comment
    //the whole svelto framework is driven by the Composition Roots created (see my articles for the definition)
    //One or more composition roots can be created inside a Context.
    //Since we are not using Unity for this example, the simplest context we can use is the Main entry point of the 
    //program
    #endregion
    public class Program
    {
        static void Main(string[] args)
        {
            simpleContext = new SimpleContext();
            
            while (true) Thread.Sleep(1000);
        }

        static SimpleContext simpleContext;
    }
    
    #region comment
    /// <summary>
    ///The Context is the framework starting point.
    ///As Composition root, it gives back to the coder the responsibility to create, 
    ///initialize and inject dependencies.
    ///Every application can have more than one context and every context can have one
    ///or more composition roots (a facade, but even a factory, can be a composition root)
    /// </summary>
    #endregion
    public class SimpleContext
    {
        #region comment
        /// <summary>
        /// Naivily we run the mainloop inside the constructor using Svelto.Tasks
        /// extension. MainApplicationLoop() is the simple extension to run whatever IEnumerator.
        /// When used outside Unity, MainApplicationLoop() starts on the Svelto.Tasks main thread.
        /// </summary>
        #endregion
        public SimpleContext()
        {
            MainApplicationLoop().Run();
        }

        IEnumerator MainApplicationLoop()
        {
            #region comment  
            //An EngineRoot holds all the engines created so far and is 
            //responsible of the injection of the entity entityViews inside every
            //relative engine.
            //Every Composition Root can have one or more EnginesRoots. Spliting
            //EnginesRoots promote even more encapsulation than the ECS paradigm
            //itself already does
            #endregion

            var simpleSubmissionEntityViewScheduler = new SimpleSubmissionEntityViewScheduler();
            _enginesRoot = new EnginesRoot(simpleSubmissionEntityViewScheduler);

            #region comment
            //an EnginesRoot must never be injected inside other classes
            //only IEntityFactory and IEntityFunctions implementation can
            #endregion
            IEntityFactory entityFactory = _enginesRoot.GenerateEntityFactory();
            IEntityFunctions entityFunctions = _enginesRoot.GenerateEntityFunctions();

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
            object[] implementors = new object[1];
            int groupID = 0;

            ProfileIt<SimpleEntityDescriptor>((entityID) =>
                      {
                          //every entity must be implemented with its own implementor obviously
                          //(otherwise the instances will be shared between entities and
                          //we don't want that, right? :) )
                          implementors[0] = new SimpleImplementor("simpleEntity");

                          //Build a SimpleEntity using specific implementors to implement the 
                          //Entity Components interfaces. The number of implementors can vary
                          //but for the purposes of this example, one is enough.
                          entityFactory.BuildEntity<SimpleEntityDescriptor>(entityID, implementors);
                      }, entityFactory);

            #region comment           
            //Entities as struct do not need an implementor. They are much more rigid
            //to use, but much faster. Please use them only when performance is really
            //critical (most of the time is not)
            #endregion
            ProfileIt<SimpleStructEntityDescriptor>((entityID) =>
                      {
                          //Build a SimpleStructEntity inside the group groupID
                          //Grouped entities can then queried by group. In a more complex scenario
                          //it would be more used than not.
                          entityFactory.BuildEntityInGroup<SimpleStructEntityDescriptor>(entityID, groupID);
                      }, entityFactory);
            
            implementors[0] = new SimpleImplementor(groupID);

            
            //Build and BuildEntityInGroup can be used either with Entity defined by implementors
            //and/or by structs
            entityFactory.BuildEntityInGroup<SimpleGroupedEntityDescriptor>(0, groupID, implementors);

            #region comment
            //quick way to submit entities, this is not the standard way, but if you
            //create a custom EntitySubmissionScheduler is up to you to decide
            //when the EntityViews are submited to the engines and DB.
            //In Unity this step is hidden by the UnitySumbmissionEntityViewScheduler
            //logic
            #endregion
            simpleSubmissionEntityViewScheduler.SubmitEntities();

            Utility.Console.Log("Done - click any button to quit");

            Console.ReadKey();
            
            Environment.Exit(0);

            yield break;
        }
        
        #region comment
        /// <summary>
        ///with Unity there is no real reason to use any different than the 
        ///provided UnitySubmissionEntityViewScheduler. However Svelto.ECS
        ///has been written to be platform agnostic, so that you can
        ///write your own scheduler on another platform.
        ///The following scheduler has been made just for the sole purpose
        ///to show the simplest execution possible, which is add entityViews
        ///in the same moment they are built.
        /// </summary>
        #endregion
        class SimpleSubmissionEntityViewScheduler : EntitySubmissionScheduler
        {
            public void SubmitEntities()
            {
                _submitEntityViews.Invoke();
            }
            
            public override void Schedule(WeakAction submitEntityViews)
            {
                _submitEntityViews = submitEntityViews;
            }
            
            WeakAction _submitEntityViews;
        }

        void ProfileIt<T>(Action<int> action, IEntityFactory entityFactory) where T : IEntityDescriptor, new()
        {
#if PROFILE            
            var watch = new System.Diagnostics.Stopwatch();
    
            entityFactory.Preallocate<T>(10000000);

            watch.Start();
            
            for (var entityID = 0; entityID < 10000000; entityID++)
                action(entityID);
#else
                action(0);
#endif                
#if PROFILE            
            watch.Stop();

            Utility.Console.Log(watch.ElapsedMilliseconds.ToString());
#endif    
        }

        EnginesRoot _enginesRoot;
    }

    namespace SimpleEntityAsClass
    {
        namespace SimpleEntity
        {
            //just a custom component as proof of concept
            public interface ISimpleComponent
            {
                string name { get; }
                int groupID { get; set;  }
            }
            
            //the implementor(s) implement the components of the Entity. In Svelto.ECS
            //components are always interfaces when Entities as classes are used
            class SimpleImplementor : ISimpleComponent
            {
                public SimpleImplementor(int groupID)
                {
                    this.groupID = groupID;
                }
                
                public SimpleImplementor(string name)
                {
                    this.name = name;
                }

                public string name { get; }
                public int groupID
                {
                    get; set;
                }
            }
            #region comment
            //The EntityDescriptor identify your Entity. It's essential to identify
            //your entities with a name that comes from the Game Design domain.
            //More about this on my articles.
            #endregion
            class SimpleEntityDescriptor : GenericEntityDescriptor<BehaviourEntityViewForSimpleEntity>
            {}
            
            class SimpleGroupedEntityDescriptor : GenericEntityDescriptor<BehaviourEntityViewForSimpleGroupedEntity>
            {}
        }

        namespace SimpleEntityEngine
        {
            #region comment
            /// <summary>
            /// In order to show as many features as possible, I created this pretty useless Engine that
            /// accepts two Entities (so I can show the use of a MultiEntitiesViewEngine).
            /// Now, keep this in mind: an Engine should seldomly inherit from SingleEntityViewEngine
            /// or MultiEngineEntityViewsEngine.
            /// The Add and Remove callbacks per EntityView submitted is another unique feature of Svelto.ECS
            /// and they are meant to be used if STRICTLY necessary. The feature has been mainly added
            /// to setup DispatchOnChange and DispatchOnSet (check my articles to know more), but
            /// it can be exploited for other reasons if well thought through!
            /// So, yes, normally engines just implement IQueryingEntityViewEngine 
            /// </summary>
            #endregion
            public class BehaviourForSimpleEntityEngine : MultiEntityViewsEngine<BehaviourEntityViewForSimpleEntity, BehaviourEntityViewForSimpleGroupedEntity>
            {
                readonly IEntityFunctions _entityFunctions;

                public BehaviourForSimpleEntityEngine(IEntityFunctions entityFunctions)
                {
                    _entityFunctions = entityFunctions;
                }

                protected override void Add(BehaviourEntityViewForSimpleEntity entity)
                {
#if !PROFILE                    
                    Utility.Console.Log("EntityView Added");
    
                    _entityFunctions.RemoveEntity<SimpleEntityDescriptor>(entity.ID);
#endif    
                }

                protected override void Remove(BehaviourEntityViewForSimpleEntity entity)
                {
                    Utility.Console.Log(entity.simpleComponent.name + "EntityView Removed");
                }

                /// <summary>
                /// With the following code I demostrate two features:
                /// First how to move an entity between groups
                /// Second how to remove an entity from a group
                /// </summary>
                /// <param name="entity"></param>
                protected override void Add(BehaviourEntityViewForSimpleGroupedEntity entity)
                {
                    Utility.Console.Log("Grouped EntityView Added");
                    
                    _entityFunctions.SwapEntityGroup<SimpleGroupedEntityDescriptor>(entity.ID, entity.simpleComponent.groupID, 1);
                    entity.simpleComponent.groupID = 1;
                    Utility.Console.Log("Grouped EntityView Swapped");
                    _entityFunctions.RemoveEntityFromGroup<SimpleGroupedEntityDescriptor>(entity.ID, entity.simpleComponent.groupID);
                }

                protected override void Remove(BehaviourEntityViewForSimpleGroupedEntity entity)
                {
                    Utility.Console.Log("Grouped EntityView Removed");
                }
            }
            #region comment
            /// <summary>
            /// You must always design an Engine according the Entities it must handle.
            /// The Entities must be real application/game entities and cannot be
            /// abstract concepts (It would be very dangerous to create abstract/meaningless
            /// entities just for the purpose to write specific logic).
            /// An EntityView is just how an Engine see a specific Entity, this allows
            /// filtering of the components and promote abstraction/encapsulation
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
                <EntityViewStructBuilder<SimpleEntityStructEngine.BehaviourEntityViewForSimpleStructEntity>>
            {}
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
            /// SingleEntityViewEngine and MultiEntityViewsEngine cannot be used with
            /// EntityView as struct as it would not make any sense. EntityViews as
            /// struct are meant to be use for tight high performance loops where
            /// cache coherence is considered during the design process
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
                    Utility.Console.Log("Task Waiting");

                    while (true)
                    {
                        var entityViews = entityViewsDB.QueryGroupedEntityViewsAsArray<BehaviourEntityViewForSimpleStructEntity>(0, out int count);

                        if (count > 0)
                        {
                            for (int i = 0; i < count; i++)
                                AddOne(ref entityViews[i].counter);

                            Utility.Console.Log("Task Done");

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

