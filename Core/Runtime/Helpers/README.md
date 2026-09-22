# Helper

[← RiseOn.Utils](../../README.md)

Lớp tĩnh gọi thẳng theo tên: `UndoHelper`, `HandlesHelper`, `MathHelper`,
`WaitForSecondCache`.

## UndoHelper

Dùng khi code (nút Odin, `OnValidate`, tool Editor) sửa object trong Edit mode:

```csharp
UndoHelper.RecordForUndo(target);    // trước khi sửa
target.name = "Renamed";
UndoHelper.MarkDirty(target);        // sau khi sửa

var go = UndoHelper.CreateGameObjectUndo("Anchor");
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
- Trong lớp kế thừa [lớp nền](../Bases/README.md), gọi thẳng `RecordForUndo` /
  `MarkDirty` của lớp đó cho gọn.

## HandlesHelper

Vẽ gizmo bằng `Handles` ngay trong code runtime, không cần `#if UNITY_EDITOR`:

```csharp
private void OnDrawGizmos() {
    var size = HandlesHelper.GetSize(TF.position);   // kích thước handle theo mức zoom
    HandlesHelper.Label(TF.position + Vector3.up * size, name, Color.yellow);   // chữ canh giữa điểm

    var old = HandlesHelper.GetMatrix();
    HandlesHelper.SetMatrix(TF.localToWorldMatrix);
    HandlesHelper.DrawRect(new Color(1, 0, 0, .25f), Color.red,
        new(0, 0), new(1, 0), new(1, 1), new(0, 1));
    HandlesHelper.SetMatrix(old);
}
```

Ngoài Editor, `GetSize` trả `1`, `GetMatrix` trả `identity`, các hàm vẽ không
làm gì.

## MathHelper

```csharp
float k    = MathHelper.Evaluate(t, 3);   // 1 - (1 - t)^3: ease-out bậc 3, t từ 0 tới 1
float cube = MathHelper.Pow(x, 3);        // lũy thừa số mũ nguyên
MathHelper.Swap(ref a, ref b);
foreach (var cell in MathHelper.IEIndex2D(0, width, 0, height)) { /* ... */ }
```

## WaitForSecondCache

`WaitForSecondCache.Get(seconds)` trả `WaitForSeconds` dùng lại theo từng giá trị
thời gian, khỏi cấp phát mỗi lần `yield`:

```csharp
yield return WaitForSecondCache.Get(0.5f);
```
