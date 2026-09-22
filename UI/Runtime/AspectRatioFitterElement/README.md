# AspectRatioFitterElement

[← RiseOn.Utils.UI](../../README.md)

Giữ tỉ lệ khung của một phần tử nằm trong `HorizontalLayoutGroup` /
`VerticalLayoutGroup`.

`AspectRatioFitter` có sẵn của Unity tự đặt kích thước RectTransform, nên giằng co
với layout group cha. Component này làm việc khác: nó là một `ILayoutElement`,
báo cho layout group kích thước tối thiểu tính theo tỉ lệ, để chính layout group
đặt kích thước.

## Cách dùng

1. Thêm component vào phần tử con của một layout group. Chế độ được chọn sẵn
   theo layout group cha: Vertical thì *Width Controls Height*, Horizontal thì
   *Height Controls Width*.
2. Đặt *Aspect Ratio* = rộng / cao (vd. `1.7778` cho 16:9).
3. Ở layout group cha, bật *Control Child Size* cho trục bị điều khiển: Height
   với *Width Controls Height*, Width với *Height Controls Width*.

| Trường | Ý nghĩa |
|---|---|
| *Aspect Mode* | `WidthControlsHeight`: cao = rộng / tỉ lệ. `HeightControlsWidth`: rộng = cao × tỉ lệ |
| *Aspect Ratio* | Rộng / cao, nhỏ nhất `0.0001` |

Trục bị điều khiển được khóa trong Inspector của RectTransform (driven), như các
component layout có sẵn.

Đổi từ code:

```csharp
fitter.AspectMode  = AspectRatioFitterElement.AspectModeId.WidthControlsHeight;
fitter.AspectRatio = 16f / 9f;   // layout được đánh dấu dựng lại ngay
```

## Lưu ý

- Component báo kích thước *tối thiểu*, *preferred* và *flexible* để trống
  (`-1`); layout group dùng giá trị tối thiểu đó làm kích thước.
- Ở frame đầu, layout được đánh dấu dựng lại thêm một lần sau một frame, để kích
  thước đúng ngay cả khi layout cha chưa ổn định lúc `Start`.
