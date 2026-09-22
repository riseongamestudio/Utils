# Extension trong Editor

[← RiseOn.Utils](../../README.md)

`PrefabExtensions`, namespace `RiseOn.Utils.Editor`:

```csharp
var root = component.GetRootPrefab();             // null nếu không thuộc prefab
if (component.TryGetRootPrefab(out var prefabRoot)) { /* ... */ }
```

Trả GameObject gốc của prefab chứa object: prefab đang mở trong prefab stage,
hoặc prefab asset. Object nằm trong scene thì trả `null`.
