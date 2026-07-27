@using System.Data
@{
    ViewData["Title"] = "Đánh giá phòng/KTX";
    DataRow pd = ViewBag.Phieu;
}

@* Đánh giá phòng | View đơn giản -> style Inline *@
@section Styles {
    <style>
        .dg-box {
            max-width: 560px;
            margin: 0 auto;
        }

        .star-rating {
            display: flex;
            flex-direction: row-reverse;
            justify-content: center;
            gap: .2rem;
            font-size: 2rem;
        }

            .star-rating input {
                display: none;
            }

            .star-rating label {
                color: #d8e2de;
                cursor: pointer;
            }

            .star-rating input:checked ~ label,
            .star-rating label:hover,
            .star-rating label:hover ~ label {
                color: #f2a516;
            }
    </style>
}

<div class="dg-box">
    <div class="card p-4">
        <h2 class="h5 fw-bold text-dm mb-1"><i class="bi bi-star-fill me-2"></i>Đánh giá phòng @pd["MaPhong"]</h2>
        <p class="text-muted small mb-3">Cảm ơn bạn đã ở tại KTX. Vui lòng chia sẻ trải nghiệm của bạn về phòng @pd["MaPhong"] để giúp cải thiện chất lượng dịch vụ.</p>

        <form method="post" asp-action="GuiDanhGia">
            <input type="hidden" name="maPhieu" value="@pd["MaPhieu"]" />

            <div class="mb-3 text-center">
                <label class="form-label small d-block mb-2">Số sao đánh giá</label>
                <div class="star-rating">
                    <input type="radio" id="s5" name="soSao" value="5" checked /><label for="s5" title="5 sao"><i class="bi bi-star-fill"></i></label>
                    <input type="radio" id="s4" name="soSao" value="4" /><label for="s4" title="4 sao"><i class="bi bi-star-fill"></i></label>
                    <input type="radio" id="s3" name="soSao" value="3" /><label for="s3" title="3 sao"><i class="bi bi-star-fill"></i></label>
                    <input type="radio" id="s2" name="soSao" value="2" /><label for="s2" title="2 sao"><i class="bi bi-star-fill"></i></label>
                    <input type="radio" id="s1" name="soSao" value="1" /><label for="s1" title="1 sao"><i class="bi bi-star-fill"></i></label>
                </div>
            </div>

            <div class="mb-3">
                <label class="form-label small">Nhận xét (không bắt buộc)</label>
                <textarea name="nhanXet" class="form-control" rows="4" placeholder="Chia sẻ cảm nhận của bạn về phòng, tiện ích, thái độ phục vụ..."></textarea>
            </div>

            <div class="d-flex gap-2">
                <a asp-action="HopDong" class="btn btn-outline-secondary w-50">Để sau</a>
                <button type="submit" class="btn btn-dm w-50">Gửi đánh giá</button>
            </div>
        </form>
    </div>
</div>
