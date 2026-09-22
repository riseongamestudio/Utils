# Utils

[← RiseOn.Utils](../../README.md)

Lớp tĩnh gọi thẳng theo tên: `UndoUtils`, `HandlesUtils`, `MathUtils`,
`WaitForSecondCache`.

## UndoUtils

Dùng khi code (nút Odin, `OnValidate`, tool Editor) sửa object trong Edit mode:

```csharp
UndoUtils.RecordForUndo(target);     // trước khi sửa
target.name = "Renamed";
UndoUtils.MarkDirty(target);         // sau khi sửa

var go = UndoUtils.CreateGameObjectUndo("Anchor");
```

| Hàm | Việc |
|---|---|
| `RecordForUndo(obj)`, `RecordForUndo(params objs)` | `Undo.RecordObject` |
| `MarkDirty(obj)`, `MarkDirty(params objs)` | `SetDirty`, ghi override của prefab instance, đánh dấu scene dirty |
| `CreateGameObjectUndo(name)` | `new GameObject` và ghi Undo cho việc tạo |

- `RecordForUndo` / `MarkDirty` bỏ qua khi đang Play và bị xóa khỏi bản build
  cùng các tham số.
- Đây là hàm tĩnh chứ không phải extension, có chủ ý: TMPro có sẵn extension
  public `MarkDirty(this Object)`, nên một extension trùng tên sẽ lỗi gọi mơ hồ ở
  mọi file có `using TMPro`.

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

## MathUtils

```csharp
float k    = MathUtils.Evaluate(t, 3);    // 1 - (1 - t)^3: ease-out bậc 3, t từ 0 tới 1
float cube = MathUtils.Pow(x, 3);         // lũy thừa số mũ nguyên
MathUtils.Swap(ref a, ref b);
foreach (var cell in MathUtils.IEIndex2D(0, width, 0, height)) { /* ... */ }
```

## WaitForSecondCache

`WaitForSecondCache.Get(seconds)` trả `WaitForSeconds` dùng lại theo từng giá trị
thời gian, khỏi cấp phát mỗi lần `yield`:

```csharp
yield return WaitForSecondCache.Get(0.5f);
```
