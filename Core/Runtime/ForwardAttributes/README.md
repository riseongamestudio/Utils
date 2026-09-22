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
    [InlineEditor] public List<Ref<EnemyConfig>> variants;   // áp cả cho mảng / list của wrapper
}
```

| Tùy chọn | Ý nghĩa |
|---|---|
| `targetNames` (tham số) | Tên member nhận attribute; khai được nhiều tên |
| `AdditionalAttributes` | Thêm kiểu attribute được chuyển, vd. `new[] { typeof(AssetsOnlyAttribute) }` |
| `IncludeProcessorDefaults` | Mặc định `true`: chuyển cả bộ mặc định `[InlineEditor]`, `[Required]`, `[PreviewField]`; đặt `false` để chỉ chuyển kiểu tự khai |

Attribute được chuyển sẽ bị gỡ khỏi field wrapper, nên không vẽ hai lần. Cần Odin
Inspector; processor nằm ở `Editor/ForwardAttributes`.
