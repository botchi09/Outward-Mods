## Outward Explorer

- Requires Partiality
- No other dependancies. Just drop in the mods folder enable it.

This is an experimental debugging tool, used to debug and explore the executing game in a similar manner to the Unity editor (though far less robust).

In the main explorer window, the top panel shows you the current Scene hierarchy. You can traverse a GameObject's children by clicking on it, and you can inspect the components on a GameObject with the "Inspect" button, or by clicking on an object which has no children.

In the bottom half of the window, there is a list of the components on the gameobject. "Inspect" a component to view a window which will use reflection to get every field on the component, including it's inherited classes. The "json" window will print out a JsonUtility.ToJson() dump.

The Prefab Editor can be used to view everything in ResourcesPrefabManager.

The Quest Events can be used as a temporary workaround for the broken F4 debug quest event menu.
