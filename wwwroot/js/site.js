function hienThiThongBaoGioHang(noiDung) {
    let thongBao = document.getElementById("cartToast");
    if (!thongBao) {
        thongBao = document.createElement("div");
        thongBao.id = "cartToast";
        thongBao.className = "fixed bottom-5 right-5 z-[80] rounded-xl bg-slate-900 px-4 py-3 text-sm font-semibold text-white shadow-xl transition-opacity";
        document.body.appendChild(thongBao);
    }

    thongBao.textContent = noiDung;
    thongBao.classList.remove("opacity-0");

    window.clearTimeout(thongBao.dataset.timer);
    thongBao.dataset.timer = window.setTimeout(() => {
        thongBao.classList.add("opacity-0");
    }, 1800);
}

async function capNhatSoLuongGio() {
    const dem = document.getElementById("cartCount");
    if (!dem) return;

    try {
        const response = await fetch("/GioHang/SoLuong", {
            headers: { "Accept": "application/json" }
        });
        const data = await response.json();
        const soLuong = Number(data.soLuong || 0);

        dem.textContent = soLuong;
        dem.classList.toggle("hidden", soLuong === 0);
    } catch {
        dem.classList.add("hidden");
    }
}

function layAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
}

async function themSanPhamVaoGio(sanPhamId) {
    const body = new URLSearchParams();
    body.append("sanPhamId", sanPhamId);

    const response = await fetch("/GioHang/Them", {
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded",
            "Accept": "application/json",
            "RequestVerificationToken": layAntiForgeryToken()
        },
        body
    });

    return response.json();
}

function ganNutThemGioHang() {
    document.querySelectorAll(".cart-add-button").forEach((nut) => {
        nut.addEventListener("click", async () => {
            try {
                const ketQua = await themSanPhamVaoGio(nut.dataset.id);
                hienThiThongBaoGioHang(ketQua.thongBao || "Đã cập nhật giỏ hàng.");

                if (ketQua.canDangNhap) {
                    window.setTimeout(() => {
                        window.location.href = "/DangNhap";
                    }, 900);
                    return;
                }

                await capNhatSoLuongGio();
            } catch {
                hienThiThongBaoGioHang("Không thể cập nhật giỏ hàng. Vui lòng thử lại.");
            }
        });
    });
}

function ganNutDoiGiaoDien() {
    const nut = document.getElementById("themeToggle");
    if (!nut) return;

    const iconSang = nut.querySelector(".theme-icon-light");
    const iconToi = nut.querySelector(".theme-icon-dark");

    const capNhatNut = () => {
        const dangToi = document.documentElement.classList.contains("dark");
        nut.setAttribute("aria-pressed", dangToi ? "true" : "false");
        nut.setAttribute("title", dangToi ? "Chuy\u1ec3n sang giao di\u1ec7n s\u00e1ng" : "Chuy\u1ec3n sang giao di\u1ec7n t\u1ed1i");
        iconSang?.classList.toggle("hidden", dangToi);
        iconToi?.classList.toggle("hidden", !dangToi);
    };

    capNhatNut();

    nut.addEventListener("click", () => {
        const dangToi = document.documentElement.classList.toggle("dark");
        localStorage.setItem("ministop-theme", dangToi ? "dark" : "light");
        capNhatNut();
    });
}

function ganMenuTaiKhoan() {
    const khung = document.querySelector("[data-account-menu]");
    if (!khung) return;

    const nut = khung.querySelector("[data-account-menu-button]");
    const menu = khung.querySelector("[data-account-menu-panel]");
    if (!nut || !menu) return;

    const dongMenu = () => {
        menu.classList.add("invisible", "opacity-0");
        nut.setAttribute("aria-expanded", "false");
    };

    const moMenu = () => {
        menu.classList.remove("invisible", "opacity-0");
        nut.setAttribute("aria-expanded", "true");
    };

    nut.addEventListener("click", (event) => {
        event.stopPropagation();
        const dangMo = nut.getAttribute("aria-expanded") === "true";
        if (dangMo) {
            dongMenu();
        } else {
            moMenu();
        }
    });

    document.addEventListener("click", (event) => {
        if (!khung.contains(event.target)) {
            dongMenu();
        }
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
            dongMenu();
        }
    });
}

function dinhDangTien(value) {
    return new Intl.NumberFormat("vi-VN").format(Number(value || 0)) + "đ";
}

function thoatHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function ganAutocompleteTimKiem() {
    const inputs = document.querySelectorAll('form[action*="TimKiem"] input[name="keyword"], input.guest-search-input[name="keyword"]');
    if (inputs.length === 0) return;

    inputs.forEach((input) => {
        let timer;
        let requestGanNhat = 0;
        const wrapper = input.closest(".search-input-wrapper") || input.closest("form");
        input.setAttribute("autocomplete", "off");
        wrapper?.classList.add("search-autocomplete-form");
        const panel = document.createElement("div");
        panel.className = "search-autocomplete-panel";
        (wrapper || input.parentElement)?.appendChild(panel);

        const dongPanel = () => {
            panel.classList.remove("is-open");
            panel.innerHTML = "";
        };

        const chonGoiY = (tenSanPham) => {
            input.value = tenSanPham;
            const url = `/TimKiem?keyword=${encodeURIComponent(tenSanPham)}`;
            window.location.href = url;
        };

        const veGoiY = (items) => {
            items = Array.isArray(items) ? items : [];
            if (!items.length) {
                panel.innerHTML = '<div class="search-autocomplete-empty">Không có gợi ý phù hợp.</div>';
                panel.classList.add("is-open");
                return;
            }

            panel.innerHTML = items.map((item) => {
                const ten = item.ten ?? item.Ten ?? "";
                const ma = item.ma ?? item.Ma ?? "";
                const giaBan = item.giaBan ?? item.GiaBan ?? 0;
                const danhMuc = item.tenDanhMuc ?? item.danhMuc ?? item.DanhMuc ?? "";
                const hinhAnh = item.hinhAnh ?? item.HinhAnh ?? "";
                const anh = hinhAnh || "https://images.unsplash.com/photo-1601598851547-4302969d0614?auto=format&fit=crop&w=120&q=80";
                const tenHtml = thoatHtml(ten);
                const maHtml = thoatHtml(ma);
                const danhMucHtml = thoatHtml(danhMuc);
                const anhHtml = thoatHtml(anh);

                return `
                    <button type="button" class="search-autocomplete-item" data-ten="${tenHtml}">
                        <img src="${anhHtml}" alt="${tenHtml}">
                        <span>
                            <strong>${tenHtml}</strong>
                            <small>${maHtml} ${danhMuc ? `- ${danhMucHtml}` : ""} - ${dinhDangTien(giaBan)}</small>
                        </span>
                    </button>
                `;
            }).join("");

            panel.querySelectorAll("[data-ten]").forEach((button) => {
                button.addEventListener("click", () => chonGoiY(button.dataset.ten || ""));
            });

            panel.classList.add("is-open");
            if (window.ministopI18n?.getLanguage?.() === "en") {
                window.ministopI18n.setLanguage("en");
            }
        };

        const xuLyNhap = () => {
            window.clearTimeout(timer);
            const tuKhoa = input.value.trim();
            if (tuKhoa.length < 1) {
                dongPanel();
                return;
            }

            timer = window.setTimeout(async () => {
                const requestHienTai = ++requestGanNhat;
                try {
                    let response = await fetch(`/Home/GoiYTimKiem?tuKhoa=${encodeURIComponent(tuKhoa)}`, {
                        cache: "no-store",
                        headers: { "Accept": "application/json" }
                    });
                    if (response.status === 404) {
                        response = await fetch(`/KhachHang/Home/GoiYTimKiem?tuKhoa=${encodeURIComponent(tuKhoa)}`, {
                            cache: "no-store",
                            headers: { "Accept": "application/json" }
                        });
                    }
                    if (!response.ok) {
                        dongPanel();
                        return;
                    }
                    const items = await response.json();
                    if (requestHienTai !== requestGanNhat) {
                        return;
                    }
                    veGoiY(items);
                } catch {
                    dongPanel();
                }
            }, 220);
        };

        input.addEventListener("input", xuLyNhap);
        input.addEventListener("focus", xuLyNhap);

        input.addEventListener("keydown", (event) => {
            if (event.key === "Escape") {
                dongPanel();
            }
        });

        document.addEventListener("click", (event) => {
            if (!panel.contains(event.target) && event.target !== input) {
                dongPanel();
            }
        });
    });
}

document.addEventListener("DOMContentLoaded", () => {
    capNhatSoLuongGio();
    ganNutThemGioHang();
    ganMenuTaiKhoan();
    ganNutDoiGiaoDien();
    ganAutocompleteTimKiem();
});
