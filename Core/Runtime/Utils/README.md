# Utils

[← RiseOn.Utils](../../README.md)

Lớp tĩnh đuôi `Utils`, tất cả trong namespace `RiseOn.Utils`. Phần lớn là
extension method, gọi thẳng trên đối tượng; phần còn lại gọi theo tên lớp.

- [Vector và Transform](#vector-và-transform)
- [Đa giác](#đa-giác)
- [Collection và Random](#collection-và-random)
- [Undo](#undo)
- [UnityEvent](#unityevent)
- [HandlesUtils](#handlesutils)
- [MathUtils và ValueUtils](#mathutils-và-valueutils)
- [Rich text](#rich-text)
- [Khác](#khác)

## Vector và Transform

`VectorUtils`, `TransformUtils`. Các hàm theo trục nhận chỉ số như indexer của
vector: `0` là x, `1` là y, `2` là z, `3` là w. Đọc hay ghi một trục của biến thì dùng
thẳng `v[i]`.

`With`, `Mul`, `Div` có cho cả `Vector2`, `Vector3`, `Vector4`, `Vector2Int`,
`Vector3Int`, kể cả kiểu mà Unity đã có toán tử, để cách gọi giống nhau ở mọi kiểu.
`Div` của kiểu `Int` chia nguyên, làm tròn về 0; muốn giữ phần lẻ thì truyền vector
thực (`cell.Div(new Vector2(2, 2))` trả `Vector2`). `SwapXY` có cho `Vector2` và `Vector2Int`.

Swizzle: mọi thứ tự của các thành phần khác nhau, trả về vector mới. Hai thành phần ra
`Vector2`, ba ra `Vector3`, bốn ra `Vector4` (kiểu `Int` ra kiểu `Int`). Không có bộ lặp
như `XX`:

```csharp
Vector2 ground = position.XZ();      // bỏ y
Vector3 flipped = position.ZYX();
Vector4 reversed = tangent.WZYX();  // tangent là Vector4
```

`TransformUtils` sửa một hay hai thành phần của `position`, `localPosition`,
`localScale`, `eulerAngles`, `localEulerAngles`, giữ nguyên phần còn lại:

| Dạng | Ví dụ |
|---|---|
| Theo chỉ số | `SetPosition(index, value)`, `AddLocalScale(index, value)` |
| Một trục | `SetPositionX(x)`, `AddLocalScaleY(y)`, `SetLocalEulerAnglesZ(z)` |
| Hai trục, nhận `Vector2` | `SetPositionXY(xy)`, `SetPositionXZ(xz)`, `AddLocalPositionYZ(yz)` |

Đủ ba trục thì dùng thẳng thuộc tính: `transform.position = v`, `transform.position += v`.

```csharp
transform.SetPositionXY(target);            // đặt x, y, giữ nguyên z
transform.SetLocalPositionXY(Vector2.zero);
transform.AddPosition(1, 0.5f);             // dời theo y
transform.SetLocalEulerAnglesZ(90);         // xoay 2D
transform.AddLocalScaleX(-0.1f);
child.ResetLocalValues();                   // localPosition 0, localRotation identity, localScale 1

var flat   = transform.position.With(2, 0);    // bản sao đổi z, dùng được trong biểu thức
var ratio  = size.Div(baseSize);               // chia từng thành phần
var area   = cell.Mul(gridSize);               // nhân từng thành phần
var turned = gridSize.YX();                    // Vector2 / Vector2Int đảo x và y
```

`SwapXY()` sửa thẳng biến (extension `ref`), nên chỉ gọi được trên biến, không gọi
trên property.

## Đa giác

`PolygonUtils`. Nới đa giác đóng, vd. cho `PolygonCollider2D`:

```csharp
var path = new List<Vector2> { a, b, c, d };   // đỉnh theo chiều kim đồng hồ
path.Inflate(0.1f);                            // mỗi cạnh dời ra 0.1, song song cạnh cũ; số âm thì thu vào
polygon.SetPath(0, path);
```

Đỉnh xếp ngược chiều kim đồng hồ thì đa giác bị thu vào.

## Collection và Random

`CollectionUtils`, `RandomUtils`.

```csharp
var clip  = clips.RandomInside();                     // phần tử ngẫu nhiên của mảng / list
var delay = new Vector2(0.5f, 1.5f).RandomInside();   // số ngẫu nhiên trong khoảng
var spawn = spawnArea.RandomInside();                 // điểm ngẫu nhiên trong BoxCollider2D (tọa độ world)
var point = center.RandomInCircle(2f);                // điểm ngẫu nhiên trong hình tròn
var speed = 3f.RandomSign();                          // 3 hoặc -3

if (items.IsEmpty()) { /* ... */ }                    // mọi tập hợp, kể cả biến kiểu IList / ICollection

foreach (var cell in grid.IEIndex2D())                // duyệt mọi ô của mảng 2 chiều, không cấp phát
    grid.ElementAt(cell) = null;                      // ElementAt trả ref, gán thẳng được

foreach (var cell in CollectionUtils.IEIndex2D(0, width, 0, height)) { /* ... */ }
```

Dùng `IList<T>` như hàng đợi hai đầu:

| Hàm | Việc |
|---|---|
| `PushBack(item)`, `PushFront(item)` | Thêm vào cuối / đầu |
| `PopBack()`, `PopFront()` | Bỏ và trả phần tử cuối / đầu; list rỗng thì ném lỗi |
| `PeekBack()`, `PeekFront()` | Trả phần tử cuối / đầu, không bỏ; list rỗng thì ném lỗi |
| `TryPopBack(out item)`, `TryPopFront`, `TryPeekBack`, `TryPeekFront` | Như trên nhưng trả `false` khi list rỗng |

Với `List<T>`, các hàm ở đầu (`PushFront`, `PopFront`) phải dời mọi phần tử còn
lại nên tốn O(n); các hàm ở cuối là O(1).

## Undo

`UndoUtils`. Dùng khi code (nút Odin, `OnValidate`, tool Editor) sửa object trong
Edit mode:

```csharp
target.RecordForUndo();              // trước khi sửa
target.name = "Renamed";
target.MarkDirty();                  // sau khi sửa

transform.EditUndo(t => t.SetPositionX(1));   // ghi Undo, sửa, đánh dấu dirty trong một lời gọi
var go = UndoUtils.CreateGameObjectUndo("Anchor");
go.transform.SetParentUndo(transform);          // như SetParent
var col = go.AddComponentUndo<BoxCollider2D>();
```

| Hàm | Việc |
|---|---|
| `obj.RecordForUndo()`, `UndoUtils.RecordForUndo(params objs)` | `Undo.RecordObject` |
| `obj.MarkDirty()`, `UndoUtils.MarkDirty(params objs)` | `SetDirty`, ghi override của prefab instance, đánh dấu scene dirty |
| `obj.EditUndo(o => ...)` | Ghi Undo, chạy thao tác sửa, đánh dấu dirty; dùng cho mọi object và mọi thao tác. Bản build chỉ chạy thao tác sửa |
| `CreateGameObjectUndo(name)` | `new GameObject` và ghi Undo cho việc tạo |
| `SetParentUndo`, `AddComponentUndo<T>` | Bản có Undo của thao tác Transform / GameObject; trong bản build chạy như hàm thường |

- `RecordForUndo` / `MarkDirty` bỏ qua khi đang Play và bị xóa khỏi bản build
  cùng các tham số.
- uGUI có sẵn extension public `MarkDirty(this Object)` trong namespace `TMPro`, nên
  file nào có `using TMPro;` gọi `obj.MarkDirty()` sẽ lỗi gọi mơ hồ. Ở file đó viết
  `UndoUtils.MarkDirty(obj)`.

## UnityEvent

`UnityEventUtils`. Thêm persistent listener (được lưu vào asset và hiện trong
Inspector như khi kéo thả tay), có Undo. Tham số đầu là object sở hữu event:

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

## HandlesUtils

Vẽ gizmo bằng `Handles` ngay trong code runtime, không cần `#if UNITY_EDITOR`:

```csharp
private void OnDrawGizmos() {
    var size = HandlesUtils.GetSize(transform.position); // kích thước handle theo mức zoom
    HandlesUtils.Label(transform.position + Vector3.up * size, name, Color.yellow); // chữ canh giữa điểm

    var old = HandlesUtils.GetMatrix();
    HandlesUtils.SetMatrix(transform.localToWorldMatrix);
    HandlesUtils.DrawRect(new Color(1, 0, 0, .25f), Color.red,
        new(0, 0), new(1, 0), new(1, 1), new(0, 1));
    HandlesUtils.SetMatrix(old);
}
```

Ngoài Editor, `GetSize` trả `1`, `GetMatrix` trả `identity`, các hàm vẽ không
làm gì.

## MathUtils và ValueUtils

```csharp
float a = MathUtils.EaseInPower(t, 3);    // t^3: chậm rồi nhanh dần (InCubic), t từ 0 tới 1
float b = MathUtils.EaseOutPower(t, 3);   // 1 - (1 - t)^3: nhanh rồi chậm dần (OutCubic)
float c = MathUtils.EaseInOutPower(t, 3); // chậm hai đầu, nhanh ở giữa (InOutCubic)
ValueUtils.Swap(ref a, ref b);

if (bestScore.RaiseTo(score)) SaveRecord();   // bestScore = max(bestScore, score), true nếu có đổi
cooldown.LowerTo(0.5f);                        // cooldown = min(cooldown, 0.5f)
unlocked.RaiseTo(Stage.Forest);               // enum cũng được
```

`RaiseTo` / `LowerTo` sửa thẳng biến (extension `ref`), nên chỉ gọi được trên biến,
không gọi trên property. Dùng được với số, enum và struct cài `IComparable`.

## Rich text

`StringUtils`: các hàm `TagXX()` bọc chữ trong thẻ rich text.

```csharp
Debug.Log($"Mất kết nối: {error}".TagColor(Color.red).TagBold());
label.text = $"Điểm {score.ToString().TagSize(48).TagColor("#FFD700")}";
title.text = name.TagNoParse();              // tên người chơi có dấu < cũng hiện nguyên
```

| Hàm | Thẻ | Đọc được ở |
|---|---|---|
| `TagBold`, `TagItalic`, `TagSize(pixels / "150%")`, `TagColor(Color / "#hex" / "red")` | `b`, `i`, `size`, `color` | Console, uGUI Text, TextMeshPro |
| `TagUnderline`, `TagStrikethrough`, `TagMark(color)` | `u`, `s`, `mark` | TextMeshPro |
| `TagSuperscript`, `TagSubscript` | `sup`, `sub` | TextMeshPro |
| `TagUppercase`, `TagLowercase`, `TagSmallCaps` | `uppercase`, `lowercase`, `smallcaps` | TextMeshPro |
| `TagNoParse`, `TagNoBreak` | `noparse`, `nobr` | TextMeshPro |
| `TagLink(id)`, `TagFont(asset)`, `TagStyle(name)`, `TagAlign(alignment)` | `link`, `font`, `style`, `align` | TextMeshPro |
| `TagCharSpacing(em)`, `TagMonospace(em)`, `TagVerticalOffset(em)` | `cspace`, `mspace`, `voffset` | TextMeshPro |
| `Tag(name)`, `Tag(name, value)` | thẻ bất kỳ | tùy thẻ |

Console và uGUI Text in các thẻ của riêng TextMeshPro ra như chữ thường. Số được ghi
theo văn hoá bất biến, nên `1.5em` không thành `1,5em` trên máy đặt locale tiếng Việt.

## Khác

| Hàm | Lớp | Việc |
|---|---|---|
| `color.WithA(a)`, `WithR`, `WithG`, `WithB`, `With(index, value)` | `ColorUtils` | Bản sao đổi một kênh; có cho cả `Color` và `Color32` |
| `text.ToUpperFirst()` | `StringUtils` | Viết hoa chữ đầu |
| `obj.IsPlaying()` | `ObjectUtils` | Đang Play thật sự: trả `false` với object trong prefab stage hay asset, kể cả khi Editor đang Play |
| `obj.PingObject()` | `ObjectUtils` | Nháy object trong Hierarchy / Project (chỉ Editor) |
| `obj.GetPath(withSceneName)` | `ObjectUtils` | Đường dẫn dễ đọc: `Scene/Parent/Child<Component>`, hoặc đường dẫn asset |
