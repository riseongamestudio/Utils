# ForwardAttributesTo

[← RiseOn.Utils](../../README.md)

Cho attribute của Odin đặt trên field kiểu wrapper có tác dụng lên field bên
trong wrapper. Hữu ích với các kiểu bọc một giá trị để serialize, như `Ref<T>`
dưới đây.

```csharp
[Serializable, ForwardAttributesTo(nameof(value))]
public struct Ref<T> where T : Object {
    public T value;
}

public class Enemy : MonoBehaviour {
    // [InlineEditor] và [Required] được chuyển xuống field `value`
    [InlineEditor, Required] public Ref<EnemyConfig> config;
    [InlineEditor] public List<Ref<EnemyConfig>> variants;   // áp cho từng phần tử của mảng / list wrapper
}
```

| Tùy chọn | Ý nghĩa |
|---|---|
| `targetNames` (tham số) | Tên member nhận attribute; khai được nhiều tên |
| `AdditionalAttributes` | Thêm kiểu attribute chỉ wrapper này chuyển |
| `IncludeProcessorDefaults` | Mặc định `true`: chuyển cả bộ mặc định bên dưới; đặt `false` để chỉ chuyển kiểu tự khai |

## Bộ mặc định

Chỉ gồm attribute nói về chính giá trị và không gọi tên member nào. Label, group,
thứ tự ở lại trên field wrapper; attribute có biểu thức gọi member của class chứa
(`ValidateInput`, `ValueDropdown`, `OnValueChanged`, `ShowIf`...) cũng ở lại, vì
xuống field bên trong thì không còn thấy class đó.

| Nhóm | Attribute |
|---|---|
| Object | `Required`, `RequiredIn`, `InlineEditor`, `PreviewField`, `AssetsOnly`, `SceneObjectsOnly`, `ChildGameObjectsOnly`, `AssetSelector`, `DisallowModificationsIn` |
| Tham chiếu | `HideReferenceObjectPicker`, `AllowGenerics` |
| Số | `MinValue`, `MaxValue`, `PropertyRange`, `MinMaxSlider`, `Wrap`, `ProgressBar`, `Unit`, `Range`, `Min` |
| Chuỗi | `Multiline`, `TextArea`, `MultiLineProperty`, `FilePath`, `FolderPath`, `DisplayAsString`, `Delayed`, `DelayedProperty` |
| Enum, màu | `EnumToggleButtons`, `EnumPaging`, `ColorUsage`, `ColorPalette` |
| Nhãn phụ | `SuffixLabel` |
| Danh sách | `ListDrawerSettings`, `TableList`, `TableMatrix`: chỉ chuyển khi chính wrapper bọc một danh sách (vd. `ListSerRef`); field là danh sách các wrapper (`List<SerRef<T>>`) giữ chúng cho mình |

Attribute được chuyển bị gỡ khỏi field wrapper, nên không vẽ hai lần. Với danh
sách các wrapper, attribute trên field danh sách chuyển xuống từng phần tử.

Project cần thêm kiểu cho mọi wrapper thì thêm vào
`ForwardAttributeProcessor.ValueAttributes` (hoặc `CollectionAttributes`) trong một
`[InitializeOnLoadMethod]` của editor, không cần sửa pack:

```csharp
[InitializeOnLoadMethod]
private static void ForwardMyAttributes() => ForwardAttributeProcessor.ValueAttributes.Add(typeof(MyRangeAttribute));
```

Cần Odin Inspector; processor nằm ở `Editor/ForwardAttributes`.
