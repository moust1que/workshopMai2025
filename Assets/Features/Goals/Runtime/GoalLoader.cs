using System.Collections.Generic;
using UnityEngine;
using System;  
using ScriptableObjectArchitecture.Runtime;

namespace Goals.Runtime {
    using BBehaviour.Runtime;

    [AddComponentMenu("Goals/Goal Loader")]
    public class GoalLoader : BBehaviour {
        [SerializeField] private TextAsset jsonFile;
        [SerializeField] private DictionaryVariable dictionary;

        private readonly Dictionary<string, IFact> factTable = new();

        private void Start() {
            if(jsonFile == null || dictionary == null) {
                Verbose("GoalLoader : références manquantes !", VerboseType.Error);
                return;
            }

            GoalFileWrapper wrapper = JsonUtility.FromJson<GoalFileWrapper>(jsonFile.text);

            foreach(FactJson factJson in wrapper.facts) {
                IFact fact = factJson.type switch {
                    "int" => new Fact<int>(factJson.id, int.Parse(factJson.initial), bool.Parse(factJson.persistent)),
                    "float" => new Fact<float>(factJson.id, float.Parse(factJson.initial), bool.Parse(factJson.persistent)),
                    "bool" => new Fact<bool>(factJson.id, bool.Parse(factJson.initial), bool.Parse(factJson.persistent)),
                    _ => null
                };
                if(fact == null) {
                    Verbose($"GoalLoader : type inconnu : {factJson.type}", VerboseType.Warning);
                    continue;
                }
                dictionary.SetFact(factJson.id, fact);
                factTable[factJson.id] = fact;
            }

            foreach(RoomJson roomJson in wrapper.rooms) {
                Verbose("GoalLoader : chargement de la salle " + roomJson.name, VerboseType.Log);
                foreach(GoalJson goalJson in roomJson.goals) {
                    if(!factTable.TryGetValue(goalJson.progress, out IFact prog)) {
                        Verbose($"GoalLoader : fact {goalJson.progress} non trouvé pour goal {goalJson.id}", VerboseType.Warning);
                        continue;
                    }

                    Goal goal = new() {
                        Id = goalJson.id,
                        Name = goalJson.name,
                        ParentId = goalJson.parentId,
                        Show = goalJson.show,
                        AlwaysHidden = goalJson.alwaysHidden,
                        Discarded = goalJson.discarded,
                        Progress = prog,
                        Comparison = goalJson.comparison,
                        Target = goalJson.target,
                        Prereq = goalJson.prereq?.ToArray() ?? Array.Empty<string>()
                    };

                    GoalsManager.instance.AddGoal(goal);
                }
            }

            GoalsManager.instance.EvaluateAndPropagate();
        }
        
        public void Increment(string factId, int delta = 1) {
            if(factTable.TryGetValue(factId, out IFact f) && f is Fact<int> fi) {
                fi.TypedValue += delta;
                GoalsManager.instance.EvaluateAndPropagate();
            }
        }

        public void SetBool(string factId, bool value) {
            if(factTable.TryGetValue(factId, out IFact f) && f is Fact<bool> fb) {
                fb.TypedValue = value;
                GoalsManager.instance.EvaluateAndPropagate();
            }
        }
    }
}