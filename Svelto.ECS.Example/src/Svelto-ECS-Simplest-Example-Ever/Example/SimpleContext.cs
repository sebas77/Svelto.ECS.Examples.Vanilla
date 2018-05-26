using System;
using System.Collections;

//shows the code to exceute to work with Entities as classes (most common case :) )
using Svelto.ECS.Vanilla.Example.EntityAsClass.SimpleEntity;
using Svelto.ECS.Vanilla.Example.EntityAsClass.SimpleEntity.SimpleEntityEngine;

//shows the code to execute to work with Entities as structs
using Svelto.ECS.Vanilla.Example.EntityAsClass.EntityAsStruct;
using Svelto.ECS.Vanilla.Example.EntityAsClass.EntityAsStruct.BehaviourForEntityStructEngine;

using Console = Utility.Console;

namespace Svelto.ECS.Vanilla.Example
{
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

        #endregion
        public SimpleContext()
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
            _enginesRoot.AddEngine(new BehaviourForEntityClassEngine(entityFunctions));
            //Add the Engine to manage the SimpleStructEntities
            _enginesRoot.AddEngine(new EntityStructEngine());

            #region comment

            //the number of implementors to use to implement the Entity components is arbitrary and it depends
            //by the modularity/reusability of the implementors.
            //You may avoid to create new implementors if you create modular/reusable ones
            //The concept of implementor is pretty unique in Svelto.ECS
            //and enable very interesting features. Refer to my articles to understand
            //more about it.
            //every entity component must be implemented with through the entity implementor
            //Entity can be built grouped. Grouped entities have more applications and can be
            //used for more focused queried. In this case the entityID and the groupID must
            //be specified.
            //Entity ID must be globally unique regardless if they are grouped or not.
            #endregion
            
            //build Entity with ID 1
            entityFactory.BuildEntity<SimpleEntityDescriptor>(1, new[] {new EntityImplementor("simpleEntity", false)});
            //build Entity with ID 0 in group 0
            entityFactory.BuildEntity<SimpleEntityDescriptor>(0, 0, new[] {new EntityImplementor("simpleGroupedEntity", true)});

            #region comment           
            //Entities as struct do not need an implementor. They are much more rigid
            //to use, but much faster. 
            //build entitystruct with ID 2 in group 0. Entities are 
            //separated by their type, but belong to the same group
            //if they have the same groupID. 
            #endregion

            Profile.It(100000, () => { entityFactory.BuildEntity<SimpleEntityStructDescriptor>(Profile.UglyCount++, 2, null); },
                () => {entityFactory.PreallocateEntitySpace<SimpleEntityStructDescriptor>(2, 1000000);});
            
            simpleSubmissionEntityViewScheduler.SubmitEntities();

            Console.Log("Done - click any button to quit");

            System.Console.ReadKey();

            Environment.Exit(0);
        }
   }

    namespace EntityAsClass
    {
        namespace SimpleEntity
        {
            //when EntityAsView as class are used Entity components are defined through interfaces
            public interface IEntityComponent
            {
                string name    { get; }
                bool isInGroup { get; }
            }

            //the implementor(s) implement(s) the component(s) of the Entity.
            class EntityImplementor : IEntityComponent
            {
                public EntityImplementor(string name, bool isInGroup)
                { 
                    this.name = name;
                    this.isInGroup = isInGroup;
                }

                public string name { get; }
                public bool isInGroup { get; }
            }

#region comment

            //The EntityDescriptor identifies your Entity. It's essential to identify
            //your entities with a name that comes from the Game Design domain.

#endregion
            class SimpleEntityDescriptor : GenericEntityDescriptor<BehaviourEntityViewStruct>
            {}

            namespace SimpleEntityEngine
            {
#region comment
    
                /// <summary>
                ///     You must always design an Engine according the Entities it must handle.
                ///     The Entities must be real application/game entities and cannot be
                ///     abstract concepts (It would be very dangerous to create abstract/meaningless
                ///     entities just for the purpose to write specific logic).
                ///     An EntityView is just how an Engine see a specific Entity, this allows
                ///     filtering components and promote abstraction/encapsulation and single responsability
                ///     EntityViews are therefore created together the Engine and for the Engine
                /// </summary>
    
#endregion
                public struct BehaviourEntityViewStruct : IEntityView
                {
                    public IEntityComponent simpleComponent;
                    public EGID ID { get; set; }
                }
               
#region comment
    
                /// <summary>
                ///     In order to show as many features as possible, I created this pretty useless Engine.
                ///     Now, keep this in mind: an Engine should seldom inherit from SingleEntityViewEngine
                ///     or MultiEngineEntityViewsEngine.
                ///     The Add and Remove callbacks per EntityView submitted is another unique feature of Svelto.ECS
                ///     and they are meant to be used if STRICTLY necessary. The feature has been mainly added
                ///     to setup DispatchOnChange and DispatchOnSet (check my articles to know more), but
                ///     it can be exploited for other reasons if well thought through!
                ///     So, yes, normally engines just implement IQueryingEntityViewEngine (see other engine below)
                /// </summary>
    
#endregion
                
                public class BehaviourForEntityClassEngine : SingleEntityEngine<BehaviourEntityViewStruct>
                {
                    readonly IEntityFunctions _entityFunctions;
    
                    public BehaviourForEntityClassEngine(IEntityFunctions entityFunctions)
                    {
                        _entityFunctions = entityFunctions;
                    }
    
                    protected override void Add(ref BehaviourEntityViewStruct entity)
                    {
                        if (entity.simpleComponent.isInGroup == false)
                        {
                            Console.Log("EntityView Added");

                            _entityFunctions.RemoveEntity(entity.ID);
                        }
                        else
                        {
                            Console.Log("Grouped EntityView Added");
    
                            _entityFunctions.SwapEntityGroup(entity.ID.entityID, 0, 1);
                            Console.Log("Grouped EntityView Swapped");
                            _entityFunctions.RemoveEntity(entity.ID.entityID, 1);    
                        }
                    }
    
                    protected override void Remove(ref BehaviourEntityViewStruct entity)
                    {
                        if (entity.simpleComponent.isInGroup == false)
                            Console.Log(entity.simpleComponent.name + "EntityView Removed");
                        else
                            Console.Log("Grouped EntityView Removed");
                    }
                }
            }
        }

        namespace EntityAsStruct
        {
#region comment
            //Entity can generate EntityView and EntityStructs at the same time
            //this is why this special descriptor provided by the framework
            //is called MixedEntityDescriptor. It's also the only way
            //to create EntityStructs.
#endregion
            class SimpleEntityStructDescriptor : GenericEntityDescriptor<EntityStruct>
            {
            }

            namespace BehaviourForEntityStructEngine
            {
                //An EntityStruct must always implement the IEntityStruct interface
                //don't worry, boxing/unboxing will never happen.
                struct EntityStruct : IEntityStruct
                {
                    public int counter;
                    public EGID ID { get; set; }
                }

                /// <summary>
                ///     EntityStruct are meant to be use for tight high performance loops where
                ///     cache coherence is considered during the design process
                /// </summary>
                public class EntityStructEngine : IQueryingEntityViewEngine
                {
                    public IEntityViewsDB entityViewsDB { private get; set; }

#region comment
                    /// <summary>
                    ///     Run() is the simplest Svelto.Tasks extension method to run whatever IEnumerator.
                    ///     if you want to know more about Svelto.Tasks, please check the relative
                    ///     repositories and articles. However you can use whatever method you prefer
                    ///     to run Engines Loop.
                    ///     Ready callback ensure that the entityViewsDB dependency is ready to be used.
                    /// </summary>
#endregion
                    public void Ready()
                    {
                        Update().Run();
                    }

                    IEnumerator Update()
                    {
                        Console.Log("Task Waiting for EntityStruct");

                        while (true)
                        {
                            var entityViews =
                                entityViewsDB
                                   .QueryEntities<EntityStruct>(0, out var count);

                            if (count > 0)
                            {
                                for (var i = 0; i < count; i++)
                                    AddOne(ref entityViews[i].counter);

                                Console.Log("Entity Struct engine executed");

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
}