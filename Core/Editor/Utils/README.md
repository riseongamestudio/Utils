# Utils trong Editor

[← RiseOn.Utils](../../README.md)

Namespace `RiseOn.Utils.Editor`.

- [PrefabUtils](#prefabutils)
- [InlineEditorImitator](#inlineeditorimitator)

## PrefabUtils

```csharp
var root = component.GetRootPrefab();             // null nếu không thuộc prefab
if (component.TryGetRootPrefab(out var prefabRoot)) { /* ... */ }
```

Trả GameObject gốc của prefab chứa object: prefab đang mở trong prefab stage,
hoặc prefab asset. Object nằm trong scene thì trả `null`.

## InlineEditorImitator

Dành cho người viết drawer Odin: vẽ editor lồng giống `[InlineEditor]` cho một
`UnityEngine.Object` nằm bên trong kiểu wrapper, nơi Odin không tự vẽ được.

```csharp
private InlineEditorImitator inline;

protected override void Initialize() {
    inline = new InlineEditorImitator(
        isGameObjectType: typeof(TValue) == typeof(GameObject),
        extractUnderlyingObject: wrapper => ((Ref<TValue>)wrapper).value);
}

protected override void DrawPropertyLayout(GUIContent label) {
    var attr = Property.GetAttribute<InlineEditorAttribute>();
    inline.DrawLayout(Property, ValueEntry, attr, label, (rect, lbl) => DrawObjectField(rect, lbl));
}

public void Dispose() => inline.Dispose();   // hủy các editor đã tạo
```

Tôn trọng các tùy chọn của `InlineEditorAttribute` (`ObjectFieldMode`, preview,
...). `drawBaseFieldAction` vẽ ô chọn object ở vùng `rect` được đưa vào.
