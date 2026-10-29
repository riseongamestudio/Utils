# Utils trong Editor

[← RiseOn.Utils](../../README.md)

Namespace `RiseOn.Utils.Editor`.

- [PrefabUtils](#prefabutils)
- [InspectorPropertyUtils](#inspectorpropertyutils)
- [RectFieldUtils](#rectfieldutils)
- [Foldout đệ quy](#foldout-đệ-quy)
- [InlineEditorImitator](#inlineeditorimitator)

## PrefabUtils

```csharp
var root = component.GetRootPrefab();             // null nếu không thuộc prefab
if (component.TryGetRootPrefab(out var prefabRoot)) { /* ... */ }
```

Trả GameObject gốc của prefab chứa object: prefab đang mở trong prefab stage,
hoặc prefab asset. Object nằm trong scene thì trả `null`.

## InspectorPropertyUtils

```csharp
property.SetExpandedRecursively(true);   // mở property và mọi property bên dưới
```

## RectFieldUtils

Vẽ giá trị của một `InspectorProperty` trên một dòng, trong rect cho sẵn chứ không theo
layout, cho drawer tự chia dòng mà các phần phải thẳng hàng tới từng pixel.

```csharp
RectFieldUtils.Split(valueRect, out var left, out var right);   // chia cột giá trị làm hai nửa
RectFieldUtils.Draw(easeProperty, left);
RectFieldUtils.Draw(curveProperty, right);
```

- `Draw` vẽ trong scope property của Unity, nên có dấu override của prefab; giá trị sửa xong
  ghi qua Odin, có Undo.
- Enum mở drop-down `ChoiceSearchWindow` có ô tìm, cuộn tới giá trị đang chọn; enum `[Flags]`
  mở danh sách bật tắt từng cờ, cửa sổ vẫn mở.
- `CanDraw(type)` cho biết kiểu có vẽ được không: số, `bool`, `string`, enum, vector, `Color`,
  `AnimationCurve`, `Gradient`, `UnityEngine.Object`.
- `Split` để một khe giữa hai nửa vừa dấu override của prefab, nên các dòng chia đôi thẳng
  cột với nhau.

## Foldout đệ quy

Alt+click một foldout bất kỳ trong Inspector của Odin thì mở hay gập cả nhánh bên dưới, như
Inspector và Hierarchy của Unity. `RecursiveFoldoutDrawer` bọc ngoài mọi property: thấy trạng
thái mở/gập của property vừa đổi trong lúc giữ Alt thì đặt cùng trạng thái cho mọi property
con. Foldout của Odin, của list và của drawer tự viết đều được, drawer không phải làm gì.

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
