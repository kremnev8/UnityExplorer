using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime;
using Il2CppSystem;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer;
using UnityExplorer.ObjectExplorer;
using UnityExplorer.UI.Panels;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets.ScrollView;

namespace ECSExtension.Panels
{
    public class WorldExplorer : UITabPanel
    {
        public ObjectExplorerPanel Parent { get; }

        public WorldExplorer(ObjectExplorerPanel parent)
        {
            Parent = parent;
            
            var @delegate = DelegateSupport.ConvertDelegate<Action<World>>(OnWorldStateChanged);
            World.WorldCreated += @delegate;
            World.WorldDestroyed += @delegate;
        }

        public override GameObject UIRoot => uiRoot;
        private GameObject uiRoot;

        public EntityTree Tree;

        //private GameObject refreshRow;
        private Dropdown sceneDropdown;
        private readonly Dictionary<World, Dropdown.OptionData> sceneToDropdownOption = new();
        
        // scene loader
        private Dropdown allWorldDropdown;
        private World _selectedWorld;
        private QueryComponentList queryComponentList;
        private Toggle includDisabledToggle;
        


        public World SelectedWorld
        {
            get => _selectedWorld;
            private set
            {
                _selectedWorld = value;
                Tree.SetWorld(value);
            }
        }

        public override string Name => "World Explorer";

        public override void Update()
        {
        }

        public void UpdateTree()
        {
            PopulateWorldDropdown(World.All.m_Source);
            Tree.RefreshData(false);
        }

        private void OnWorldSelectionDropdownChanged(int value)
        {
            if (value < 0 || World.All.m_Source.Count <= value)
                return;

            SelectedWorld = World.All.m_Source._items[value];
            Tree.RefreshData(true);
        }

        private void SceneHandler_OnInspectedSceneChanged(World world)
        {
            if (!sceneToDropdownOption.ContainsKey(world))
                PopulateWorldDropdown(World.All.m_Source);

            if (sceneToDropdownOption.ContainsKey(world))
            {
                Dropdown.OptionData opt = sceneToDropdownOption[world];
                int idx = sceneDropdown.options.IndexOf(opt);
                if (sceneDropdown.value != idx)
                    sceneDropdown.value = idx;
                else
                    sceneDropdown.captionText.text = opt.text;
            }
        }

        private void OnWorldStateChanged(World world)
        {
            UpdateTree();
        }

        private void PopulateWorldDropdown(Il2CppSystem.Collections.Generic.List<World> loadedScenes)
        {
            sceneToDropdownOption.Clear();
            sceneDropdown.options.Clear();

            foreach (World world in loadedScenes)
            {
                if (sceneToDropdownOption.ContainsKey(world))
                    continue;

                string name = world.Name?.Trim();
                
                if (string.IsNullOrEmpty(name))
                    name = "<untitled>";

                Dropdown.OptionData option = new(name);
                sceneDropdown.options.Add(option);
                sceneToDropdownOption.Add(world, option);
            }
        }

        
        private void DoQuery()
        {
            ComponentType[] components = queryComponentList.GetComponentTypes();
            bool includeDisabled = includDisabledToggle.isOn;
            Tree.UseQuery(components, includeDisabled);
            Tree.RefreshData(true);
        }
        

        public override void ConstructUI(GameObject content)
        {
            uiRoot = UIFactory.CreateUIObject("WorldExplorer", content);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(uiRoot, true, true, true, true, 0, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(uiRoot, flexibleHeight: 9999);

            // Tool bar (top area)

            GameObject toolbar = UIFactory.CreateVerticalGroup(uiRoot, "Toolbar", true, true, true, true, 2, new Vector4(2, 2, 2, 2),
               new Color(0.15f, 0.15f, 0.15f));

            // Scene selector dropdown

            GameObject dropRow = UIFactory.CreateHorizontalGroup(toolbar, "DropdownRow", true, true, true, true, 5, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(dropRow, minHeight: 25, flexibleWidth: 9999);

            Text dropLabel = UIFactory.CreateLabel(dropRow, "SelectorLabel", "World:", TextAnchor.MiddleLeft, Color.cyan, false, 15);
            UIFactory.SetLayoutElement(dropLabel.gameObject, minHeight: 25, minWidth: 60, flexibleWidth: 0);

            GameObject dropdownObj = UIFactory.CreateDropdown(dropRow, "SceneDropdown", out sceneDropdown, "<notset>", 13, OnWorldSelectionDropdownChanged);
            UIFactory.SetLayoutElement(dropdownObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            //SceneHandler.Update();
            PopulateWorldDropdown(World.All.m_Source);
            sceneDropdown.captionText.text = sceneToDropdownOption.First().Value.text;

            // Filter row
            
            GameObject filterRow = UIFactory.CreateVerticalGroup(toolbar, "FilterGroup", true, false, true, true, 2, new Vector4(2, 2, 2, 2),
                new Color(0.15f, 0.15f, 0.15f));

            UIFactory.SetLayoutElement(filterRow, minHeight: 75);
            
            var queryComponentScroll = UIFactory.CreateScrollPool<QueryComponentCell>(filterRow, "QueryList", out GameObject uiRoot1,
                out GameObject compContent, new Color(0.11f, 0.11f, 0.11f));
            UIFactory.SetLayoutElement(uiRoot1, minHeight: 25,  flexibleHeight: -1);
            UIFactory.SetLayoutElement(compContent, minHeight: 25, flexibleHeight: -1);
            
            GameObject viewPort = compContent.transform.parent.gameObject;
            LayoutElement layoutElement = viewPort.GetComponent<LayoutElement>();
            layoutElement.flexibleHeight = -1;
            
            queryComponentList = new QueryComponentList(queryComponentScroll, layoutElement);
            
            GameObject isDisabledToggle = UIFactory.CreateToggle(filterRow, "IncludeDisabled", out includDisabledToggle, out Text toggleText);
            UIFactory.SetLayoutElement(isDisabledToggle, minHeight: 25, minWidth: 80);
            toggleText.text = "Include disabled";
            toggleText.color = Color.grey;
            includDisabledToggle.isOn = false;
            
            ButtonRef queryButton = UIFactory.CreateButton(filterRow, "QueryButton", "Query");
            UIFactory.SetLayoutElement(queryButton.Component.gameObject, minHeight: 25, flexibleHeight: 0);
            queryButton.OnClick += DoQuery;

            // tree labels row

            GameObject labelsRow = UIFactory.CreateHorizontalGroup(toolbar, "LabelsRow", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(labelsRow, minHeight: 30, flexibleHeight: 0);

            Text nameLabel = UIFactory.CreateLabel(labelsRow, "NameLabel", "Name", TextAnchor.MiddleLeft, color: Color.grey);
            UIFactory.SetLayoutElement(nameLabel.gameObject, flexibleWidth: 9999, minHeight: 25);

            Text indexLabel = UIFactory.CreateLabel(labelsRow, "IndexLabel", "Sibling Index", TextAnchor.MiddleLeft, fontSize: 12, color: Color.grey);
            UIFactory.SetLayoutElement(indexLabel.gameObject, minWidth: 100, flexibleWidth: 0, minHeight: 25);

            // Transform Tree

            ScrollPool<EntityCell> scrollPool = UIFactory.CreateScrollPool<EntityCell>(uiRoot, "EntityTree", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.11f, 0.11f, 0.11f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(scrollContent, flexibleHeight: 9999);

            Tree = new EntityTree(scrollPool, OnCellClicked);
            SelectedWorld = World.All.m_Source._items.First();
            Tree.RefreshData(true);
        }

        void OnCellClicked(Entity obj) => InspectorManager.Inspect(obj);
    }
}