# Extension

[← RiseOn.Utils](../../README.md)

Extension method cho các kiểu hay dùng, tất cả trong namespace `RiseOn.Utils`.

- [Vector và Transform](#vector-và-transform)
- [Collection và Random](#collection-và-random)
- [Coroutine](#coroutine)
- [DOTween](#dotween)
- [Undo](#undo)
- [UnityEvent](#unityevent)
- [Khác](#khác)
- [Lưu ý](#lưu-ý)

## Vector và Transform

`VecAxis` (`X`, `Y`, `Z`) chọn trục cho các hàm theo trục.

```csharp
transform.SetPositionXY(target);            // đặt x, y, giữ nguyên z
transform.SetLocalPositionXY(Vector2.zero);
transform.AddPosition(VecAxis.Y, 0.5f);
transform.AddPositionXY(new Vector2(1, 0));
child.ResetLocalValues();                   // localPosition 0, localRotation identity, localScale 1

var flat   = transform.position.With(VecAxis.Z, 0); // bản sao đổi một trục
var y      = transform.position.Get(VecAxis.Y);
var ratio  = size.Div(baseSize);               // chia từng thành phần
var turned = gridSize.YX();                    // Vector2 / Vector2Int đảo x và y
```

`Set(axis, value)` và `SwapXY()` sửa thẳng biến (extension `ref`), nên chỉ gọi
được trên biến, không gọi trên property.

Nới đa giác đóng, vd. cho `PolygonCollider2D`:

```csharp
var path = new List<Vector2> { a, b, c, d };   // đỉnh theo chiều kim đồng hồ
path.ExpandPath(0.1f);                         // mỗi cạnh dời ra 0.1, song song cạnh cũ
polygon.SetPath(0, path);
```

Đỉnh xếp ngược chiều kim đồng hồ thì đa giác bị thu vào.

## Collection và Random

```csharp
var clip  = clips.RandomInside();                     // phần tử ngẫu nhiên của mảng / list
var delay = new Vector2(0.5f, 1.5f).RandomInside();   // số ngẫu nhiên trong khoảng
var spawn = spawnArea.RandomInside();                 // điểm ngẫu nhiên trong BoxCollider2D (tọa độ world)
var point = center.RandomInCircle(2f);                // điểm ngẫu nhiên trong hình tròn
var speed = 3f.RandomSign();                          // 3 hoặc -3

if (items.IsEmpty()) { /* ... */ }
var last = stack.PopFront();                          // bỏ và trả phần tử CUỐI

foreach (var cell in grid.IEIndex2D())                // duyệt mọi ô của mảng 2 chiều
    grid.ItemAt(cell) = null;                         // ItemAt trả ref, gán thẳng được
```

## Coroutine

Gọi trên một `MonoBehaviour`, cần viết `this.`. Hàm nào cũng trả `Coroutine` để có
thể `StopCoroutine`:

```csharp
this.DelayedCall_Second(1f, ShowResult);              // sau 1 giây
this.DelayedCall_Frame(1, RefreshLayout);             // sau 1 frame
this.DelayedCall_Cond(() => isLoaded, StartGame);     // khi điều kiện thành đúng
this.WaitForSeconds(2f, t => bar.fillAmount = t);     // t chạy từ 0 tới 1 trong 2 giây
```

## DOTween

Cần DOTween trong project (đã chạy *Setup DOTween*).

```csharp
// Nhảy tới endPos (world). Đỉnh cao hơn điểm cao nhất của hai đầu một đoạn jumpHeight,
// thời gian lên / xuống chia theo công thức rơi tự do.
transform.DOJump_BetterHeight(endPos, jumpHeight: 1f, duration: 0.5f, onReachTop: PlayWhoosh);

// Như trên, nhưng đích là độ lệch so với vị trí của cha, bỏ qua rotation và scale của cha.
transform.DOLocalJumpPure(offsetFromParent, 1f, 0.5f);

// Rung lần lượt với từng độ mạnh, tổng thời gian chia đều.
transform.DOShakePositionDynamic(0.6f, new[] { Vector3.one * 0.3f, Vector3.one * 0.1f });

// Giá trị của một kiểu Ease tại t (0 tới 1).
float k = Ease.OutBack.Evaluate(t);
```

Các tween đều gắn target là transform, nên `DOTween.Kill(transform)` dừng được.

## Undo

Bản có Undo của vài thao tác Transform / GameObject. Trong bản build chúng chạy
như hàm thường:

```csharp
go.transform.SetParentUndo(transform);       // như SetParent
var col = go.AddComponentUndo<BoxCollider2D>();
transform.AddPositionXYUndo(Vector2.up);     // dời, ghi Undo và đánh dấu dirty
```

Tạo GameObject, ghi Undo cho object tùy ý: [`UndoUtils`](../Utils/README.md#undoutils).

## UnityEvent

Thêm persistent listener (được lưu vào asset và hiện trong Inspector như khi kéo
thả tay), có Undo. Tham số đầu là object sở hữu event:

```csharp
[SerializeField] private UnityEvent<bool> onToggle;
[SerializeField] private UnityEvent onOpened, onClosed;

[Sirenix.OdinInspector.Button]
private void Wire() {
    onToggle.AddEvent(this, panel.SetActive);            // nhận tham số của event
    onOpened.AddBoolEvent(this, panel.SetActive, true);  // tham số cố định
    onClosed.AddVoidEvent(this, Close);                  // hàm không tham số
}
```

| Hàm | Việc |
|---|---|
| `AddEvent` | Listener động, nhận tham số của event (0 tới 4 tham số) |
| `AddVoidEvent`, `AddIntEvent`, `AddFloatEvent`, `AddBoolEvent`, `AddStringEvent`, `AddObjectEvent` | Listener tĩnh với tham số cố định |
| `RegisterEvent`, `Register{Void,Int,Float,Bool,String,Object}Event` | Ghi đè listener ở một index |
| `RemoveEvent` | Gỡ theo index hoặc theo hàm |
| `UnregisterEvent` | Xóa hàm ở một index, giữ chỗ |

- `unique` (mặc định `true`) gỡ listener cùng hàm trước khi thêm, tránh trùng.
- Hàm đích phải thuộc một `UnityEngine.Object`, như mọi persistent listener.
- Chỉ chạy trong Editor; bản build bỏ qua lời gọi.

## Khác

| Hàm | Việc |
|---|---|
| `color.WithAlpha(a)` | Bản sao đổi alpha |
| `text.ToUpperFirst()` | Viết hoa chữ đầu |
| `field.SetEnumIfBigger(value)` | Gán khi giá trị mới lớn hơn (extension `ref`) |
| `obj.IsPlaying()` | Đang Play thật sự: trả `false` với object trong prefab stage hay asset, kể cả khi Editor đang Play |
| `obj.PingObject()` | Nháy object trong Hierarchy / Project (chỉ Editor) |
| `obj.GetPath(withSceneName)` | Đường dẫn dễ đọc: `Scene/Parent/Child<Component>`, hoặc đường dẫn asset |

## Lưu ý

- `PopFront` bỏ và trả về phần tử **cuối** list.
- Unity Visual Scripting (phần Editor) cũng có `WithAlpha(this Color, float)`
  trong namespace `Unity.VisualScripting`. File nào có `using Unity.VisualScripting;`
  sẽ báo gọi mơ hồ; bỏ using đó là hết.
- `DOLocalJumpPure` đọc vị trí của cha ở mọi frame, nên transform phải có cha.
