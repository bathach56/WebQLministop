const ministopI18n = (() => {
    const storageKey = "ministop-language";
    const defaultLanguage = "vi";

    const dictionary = new Map(Object.entries({
        "Trang chủ": "Home",
        "Đồ uống": "Drinks",
        "Bánh kẹo": "Snacks and candy",
        "Thực phẩm": "Food",
        "Gia dụng": "Household",
        "Giá tốt": "Best price",
        "Tìm đồ uống, bánh kẹo, mì gói...": "Search drinks, snacks, instant noodles...",
        "Tìm kiếm": "Search",
        "Giỏ hàng": "Cart",
        "Đăng nhập": "Sign in",
        "Đăng ký": "Sign up",
        "Đăng xuất": "Sign out",
        "Thông tin cá nhân": "Profile",
        "Lịch sử mua hàng": "Order history",
        "Điểm tích lũy": "Reward points",
        "điểm": "points",
        "1 điểm = 1đ giảm": "1 point = 1 VND discount",
        "Trang chủ khách": "Customer home",
        "Mới ra mắt": "New arrival",
        "Mua nhanh đồ tiện lợi": "Quick convenience shopping",
        "cho mỗi ngày": "for every day",
        "Đồ uống, bánh kẹo, thực phẩm nhanh và đồ dùng cá nhân được gom theo danh mục để khách chọn dễ hơn.": "Drinks, snacks, quick food, and personal items are grouped by category for easier shopping.",
        "Khám phá ngay": "Explore now",
        "Danh mục mua nhanh": "Quick categories",
        "Xem tất cả": "View all",
        "Sản phẩm mới ra mắt": "New products",
        "Sản phẩm bán chạy": "Best sellers",
        "Trở thành thành viên MiniStop": "Become a MiniStop member",
        "Đăng ký ngay để nhận ưu đãi độc quyền, tích điểm mỗi lần mua và nhận quà tặng sinh nhật.": "Sign up for exclusive offers, reward points on every purchase, and birthday gifts.",
        "Đăng ký ngay": "Sign up now",
        "Giỏ hàng của bạn": "Your cart",
        "Tiếp tục mua hàng": "Continue shopping",
        "Giỏ hàng đang trống": "Your cart is empty",
        "Bạn có thể thêm sản phẩm từ trang chủ hoặc trang tìm kiếm.": "You can add products from the home page or search page.",
        "Về trang chủ": "Back to home",
        "Tóm tắt giỏ hàng": "Cart summary",
        "Tạm tính": "Subtotal",
        "Khuyến mãi": "Promotion",
        "Tổng cộng": "Total",
        "Mã giảm giá": "Promo code",
        "Áp dụng": "Apply",
        "Bỏ mã giảm giá": "Remove promo code",
        "Bỏ điểm tích lũy": "Remove reward points",
        "Tạo đơn hàng": "Create order",
        "Xóa toàn bộ": "Clear all",
        "Xóa": "Remove",
        "Còn": "Stock",
        "Tối đa": "Maximum",
        "Kết quả tìm kiếm": "Search results",
        "Đường dẫn điều hướng": "Breadcrumb",
        "Bộ lọc": "Filters",
        "Từ khóa": "Keyword",
        "Nhập tên sản phẩm": "Enter product name",
        "Danh mục": "Category",
        "Tất cả danh mục": "All categories",
        "Sắp xếp": "Sort",
        "Mới nhất": "Newest",
        "Giá tăng dần": "Price low to high",
        "Giá giảm dần": "Price high to low",
        "Kết quả sản phẩm": "Product results",
        "Tìm thấy": "Found",
        "sản phẩm": "products",
        "Chưa có sản phẩm phù hợp": "No matching products",
        "Bạn thử đổi từ khóa hoặc chọn danh mục khác.": "Try changing the keyword or choosing another category.",
        "Đăng nhập để tiếp tục đúng vai trò.": "Sign in to continue with the right role.",
        "Khách hàng mua sắm và tích điểm, nhân viên bán hàng tại quầy, quản lý theo dõi vận hành cửa hàng.": "Customers shop and earn points, staff sell at the counter, and managers monitor store operations.",
        "Khách hàng: giỏ hàng, lịch sử mua hàng, điểm tích lũy": "Customer: cart, order history, reward points",
        "Nhân viên: tạo hóa đơn và theo dõi tồn kho tại quầy": "Staff: create invoices and track counter stock",
        "Quản lý: xem dashboard, sản phẩm, đơn hàng và nhân sự": "Manager: view dashboard, products, orders, and staff",
        "Trang đăng nhập": "Sign-in page",
        "Chào mừng bạn quay lại": "Welcome back",
        "Khách hàng, nhân viên và quản lý đều đăng nhập tại đây bằng email hoặc số điện thoại.": "Customers, staff, and managers sign in here with email or phone number.",
        "Email hoặc số điện thoại": "Email or phone number",
        "Mật khẩu": "Password",
        "Tối thiểu 6 ký tự": "At least 6 characters",
        "Ghi nhớ đăng nhập": "Remember me",
        "Quên mật khẩu?": "Forgot password?",
        "Chưa có tài khoản?": "No account yet?",
        "Đăng ký ngay": "Sign up now",
        "hoặc": "or",
        "Đăng nhập bằng Google": "Sign in with Google",
        "Tạo tài khoản": "Create account",
        "Đăng ký thành viên để nhận ưu đãi mỗi ngày.": "Register as a member to get daily offers.",
        "Mở khóa chương trình tích điểm, ưu đãi theo mùa và những khuyến mãi dành riêng cho khách hàng thân thiết.": "Unlock reward points, seasonal deals, and promotions for loyal customers.",
        "Nhận mã giảm giá đầu tiên": "Get your first discount code",
        "Theo dõi chương trình khuyến mãi": "Follow promotions",
        "Quản lý thông tin cá nhân nhanh chóng": "Manage personal information quickly",
        "Trang đăng ký": "Sign-up page",
        "Tạo tài khoản mới": "Create a new account",
        "Điền thông tin bên dưới để bắt đầu trải nghiệm.": "Fill in the information below to get started.",
        "Họ và tên": "Full name",
        "Số điện thoại": "Phone number",
        "Email": "Email",
        "Xác nhận mật khẩu": "Confirm password",
        "Nhập lại mật khẩu": "Re-enter password",
        "Đăng ký tài khoản": "Create account",
        "Đã có tài khoản?": "Already have an account?",
        "Tổng quan": "Overview",
        "Tổng quan cửa hàng": "Store overview",
        "MiniStop nội bộ": "MiniStop internal",
        "Quản lý cửa hàng": "Store manager",
        "Nhân viên bán hàng": "Sales staff",
        "Bảng quản lý": "Management dashboard",
        "Màn hình nhân viên": "Staff screen",
        "Vai trò": "Role",
        "Quản lý": "Manager",
        "Nhân viên": "Staff",
        "Bán hàng tại quầy": "Counter sales",
        "Sản phẩm": "Products",
        "Đơn hàng": "Orders",
        "Khách hàng": "Customers",
        "Nhà cung cấp": "Suppliers",
        "Doanh thu hôm nay": "Today's revenue",
        "Số hóa đơn": "Invoices",
        "Sản phẩm sắp hết": "Low-stock products",
        "Nhân viên hoạt động": "Active staff",
        "Doanh thu 7 ngày": "7-day revenue",
        "Xuất báo cáo": "Export report",
        "Sản phẩm bán chạy": "Best-selling products",
        "Chưa có dữ liệu bán chạy": "No best-seller data yet",
        "Đơn hàng gần đây": "Recent orders",
        "Cảnh báo tồn kho": "Stock alerts",
        "Khuyến mãi đang chạy": "Active promotions",
        "Quản lý sản phẩm": "Product management",
        "Thêm sản phẩm": "Add product",
        "Tên hoặc mã sản phẩm": "Product name or code",
        "Trạng thái": "Status",
        "Giá tối đa": "Maximum price",
        "Ảnh": "Image",
        "Mã SP": "SKU",
        "Tên sản phẩm": "Product name",
        "Giá bán": "Price",
        "Tồn kho": "Stock",
        "Hành động": "Actions",
        "Sửa": "Edit",
        "Ngừng bán": "Disable",
        "Bật bán": "Enable",
        "Quản lý danh mục": "Category management",
        "Tên danh mục": "Category name",
        "Mô tả": "Description",
        "Thêm danh mục": "Add category",
        "Tổng danh mục": "Total categories",
        "Danh mục có hàng": "Categories with products",
        "Sản phẩm đã phân loại": "Categorized products",
        "Trung bình": "Average",
        "Quản lý khách hàng": "Customer management",
        "Quản lý nhân viên": "Staff management",
        "Thêm nhân viên": "Add staff",
        "Chức vụ": "Position",
        "Lương": "Salary",
        "Ngày vào làm": "Start date",
        "Quản lý nhà cung cấp": "Supplier management",
        "Thêm nhà cung cấp": "Add supplier",
        "Tên liên hệ": "Contact name",
        "Địa chỉ": "Address",
        "Quản lý khuyến mãi": "Promotion management",
        "Thêm khuyến mãi": "Add promotion",
        "Mã": "Code",
        "Phần trăm giảm": "Discount percent",
        "Ngày bắt đầu": "Start date",
        "Ngày kết thúc": "End date",
        "Màn hình nhân viên": "Staff screen",
        "Lập hóa đơn": "Create invoice",
        "Tra cứu sản phẩm": "Product lookup",
        "Thanh toán": "Checkout",
        "Tổng thanh toán": "Total payment",
        "Lịch sử giao dịch": "Transaction history",
        "Tồn kho tại quầy": "Counter stock",
        "Làm mới": "Refresh",
        "Khách lẻ": "Walk-in customer",
        "Nhập hoặc quét mã sản phẩm": "Enter or scan product code",
        "Tìm theo mã hoặc tên sản phẩm": "Search by code or product name",
        "Chưa có sản phẩm nào trong hóa đơn.": "No products in the invoice yet.",
        "Hóa đơn gần đây": "Recent invoices",
        "Thời gian": "Time",
        "Tổng tiền": "Total",
        "Chưa rõ": "Unknown",
        "Đang bán": "Active",
        "Sắp hết": "Low stock",
        "Hết hàng": "Out of stock",
        "Thành công": "Completed",
        "Chờ xử lý": "Pending",
        "Đã thanh toán": "Paid",
        "Đang xử lý": "Processing",
        "Đã hủy": "Cancelled"
    }));

    function suaChuBiLoiBangMaHoa(value) {
        try {
            const fixed = decodeURIComponent(escape(value));
            return fixed && fixed !== value ? fixed : value;
        } catch {
            return value;
        }
    }

    const boSungTuDienChuan = new Map(Object.entries({
        // Categories
        "Hóa mỹ phẩm": "Cosmetics",
        "Văn phòng phẩm": "Stationery",
        "Thức ăn nhanh": "Fast food",
        
        // Product Keywords
        "Nước tăng lực": "Energy drink",
        "Nước khoáng": "Mineral water",
        "Trà xanh": "Green tea",
        "Trà đào": "Peach tea",
        "Nước ép cam": "Orange juice",
        "Sữa tươi": "Fresh milk",
        "Bánh quy": "Biscuit",
        "Kẹo mút": "Lollipop",
        "Mì ly": "Cup noodle",
        "Xúc xích": "Sausage",
        "Cá hộp": "Canned fish",
        "Thịt hộp": "Canned meat",
        "Bánh tráng trộn": "Mixed rice paper",
        "Khăn giấy": "Tissue",
        "Bột giặt": "Washing powder",
        "Nước rửa chén": "Dishwashing liquid",
        "Giấy vệ sinh": "Toilet paper",
        "Nước lau sàn": "Floor cleaner",
        "Dầu gội": "Shampoo",
        "Sữa tắm": "Body wash",
        "Kem đánh răng": "Toothpaste",
        "Bàn chải đánh răng": "Toothbrush",
        "Sữa rửa mặt": "Facial cleanser",
        "Bút bi": "Ballpoint pen",
        "Vở học sinh": "Student notebook",
        "Băng keo": "Adhesive tape",
        "Kéo cắt giấy": "Scissors",
        "Hộp bút": "Pencil case",
        "Bánh bao": "Steamed bun",
        "Bánh mì": "Baguette",
        "Lẩu ly": "Cup hotpot",
        "Lon": "Can",
        "Chai": "Bottle",
        "Hộp": "Box",
        "Cái": "Piece",
        "Gói": "Pack",
        "Cuộn": "Roll",
        "Tuýp": "Tube",
        "Quyển": "Notebook",
        "Ly": "Cup",

        "Quản lý cửa hàng": "Store management",
        "Đã đăng ký bản quyền.": "All rights reserved.",
        "Danh mục nhanh": "Quick categories",
        "Tất cả": "All",
        "Tất cả trạng thái": "All statuses",
        "Tất cả nhân viên": "All staff",
        "Dữ liệu lấy từ bảng SanPhams.": "Data from the SanPhams table.",
        "Dữ liệu lấy từ bảng DonHangs và ChiTietDonHangs.": "Data from DonHangs and ChiTietDonHangs.",
        "Dữ liệu lấy từ bảng KhachHangs.": "Data from the KhachHangs table.",
        "Dữ liệu lấy từ bảng NhanViens.": "Data from the NhanViens table.",
        "Dữ liệu lấy từ bảng NhaCungCaps và liên kết với SanPhams.": "Data from NhaCungCaps and linked products.",
        "Dữ liệu hóa đơn sẽ xuất hiện sau khi hệ thống phát sinh đơn hàng.": "Invoice data will appear after orders are created.",
        "Dữ liệu khách hàng sẽ xuất hiện khi có hồ sơ trong hệ thống.": "Customer data will appear when profiles exist in the system.",
        "Hóa đơn sẽ hiển thị tại đây khi được tạo.": "Invoices will appear here after they are created.",
        "Khi có hóa đơn, danh sách sản phẩm bán chạy sẽ hiển thị tại đây.": "Best-selling products will appear here once invoices are created.",
        "Thêm danh mục để phân loại sản phẩm trong cửa hàng.": "Add categories to classify store products.",
        "Thêm nhân viên để quản lý hồ sơ và phân quyền.": "Add staff to manage profiles and permissions.",
        "Khi thêm nhà cung cấp, quản lý có thể theo dõi nguồn hàng tại đây.": "After adding suppliers, managers can track supply sources here.",
        "Tạo chương trình đầu tiên để bắt đầu quản lý ưu đãi.": "Create the first program to start managing offers.",
        "Tạo chương trình khuyến mãi để hiển thị tại đây.": "Create a promotion program to display it here.",
        "Khách hàng này chưa phát sinh đơn trong hệ thống.": "This customer has not created any orders in the system.",
        "Chưa có dữ liệu bán chạy": "No best-seller data yet",
        "Chưa có hóa đơn": "No invoices yet",
        "Chưa có danh mục": "No categories yet",
        "Chưa có đơn hàng": "No orders yet",
        "Chưa có khách hàng": "No customers yet",
        "Chưa có nhân viên": "No staff yet",
        "Chưa có nhà cung cấp": "No suppliers yet",
        "Chưa có khuyến mãi": "No promotions yet",
        "Chưa cập nhật": "Not updated",
        "Chưa có ghi chú thanh toán.": "No payment note yet.",
        "Chưa phân loại": "Uncategorized",
        "Chưa gán": "Unassigned",
        "Chưa rõ": "Unknown",
        "Đang hoạt động": "Active",
        "Đang kích hoạt": "Active",
        "Đang bán": "Active",
        "Đang chạy": "Active",
        "Đang xử lý": "Processing",
        "Đang tìm": "Searching",
        "Đang tìm...": "Searching...",
        "Đang tìm sản phẩm...": "Searching products...",
        "Đang lưu...": "Saving...",
        "Đang chờ xác thực SePAY": "Waiting for SePAY verification",
        "Đã thanh toán": "Paid",
        "Đã hủy": "Cancelled",
        "Đã kết thúc": "Ended",
        "Thành công": "Completed",
        "Chờ xử lý": "Pending",
        "Còn hàng": "In stock",
        "Sắp hết": "Low stock",
        "Sắp hết hạn": "Ending soon",
        "Hết hàng": "Out of stock",
        "Ngừng": "Disabled",
        "Ngừng bán": "Disable",
        "Kích hoạt": "Activate",
        "Bật bán": "Enable",
        "Bật": "Enable",
        "Tắt": "Disable",
        "Lưu": "Save",
        "Lưu role": "Save role",
        "Lưu quyền": "Save permissions",
        "Lưu sản phẩm": "Save product",
        "Cập nhật sản phẩm": "Update product",
        "Lưu nhân viên": "Save staff",
        "Lưu nhà cung cấp": "Save supplier",
        "Lưu khuyến mãi": "Save promotion",
        "Sửa sản phẩm": "Edit product",
        "Xem chi tiết": "View details",
        "Chi tiết": "Details",
        "Chi tiết hóa đơn": "Invoice details",
        "Chi tiết khách hàng": "Customer details",
        "Chọn một hóa đơn": "Select an invoice",
        "Chọn khách hàng": "Select a customer",
        "Thông tin hiển thị": "Display information",
        "Thông tin nhân viên": "Staff information",
        "Thông tin nhà cung cấp": "Supplier information",
        "Thông tin khuyến mãi": "Promotion information",
        "Thông tin giao hàng": "Delivery information",
        "Giao hàng & thanh toán": "Delivery and payment",
        "Địa chỉ nhận hàng": "Delivery address",
        "Địa chỉ giao hàng": "Delivery address",
        "Phương thức thanh toán": "Payment method",
        "Thanh toán tiền mặt": "Cash on delivery",
        "Thanh toán bằng tiền mặt cho shipper khi nhận được hàng.": "Pay cash to the shipper when receiving the order.",
        "Chuyển khoản QR SePAY": "SePAY QR transfer",
        "Chuyển khoản QR": "QR transfer",
        "QR SePAY": "SePAY QR",
        "Tiền mặt": "Cash",
        "Mở app ngân hàng quét mã QR. Đơn tự xác nhận sau vài giây.": "Open your banking app and scan the QR code. The order is confirmed after a few seconds.",
        "Thanh toán an toàn & bảo mật": "Safe and secure payment",
        "Đơn sẽ được thu tiền mặt khi hàng giao đến.": "Cash will be collected on delivery.",
        "Vui lòng quét mã QR SePAY và chờ nhân viên xác nhận tin nhắn giao dịch.": "Please scan the SePAY QR code and wait for staff to confirm the transaction message.",
        "Vui lòng xác nhận thanh toán trước khi in hóa đơn": "Please confirm payment before printing the invoice",
        "Nội dung": "Content",
        "Số tiền": "Amount",
        "Kênh bán": "Sales channel",
        "Kênh": "Channel",
        "Website": "Website",
        "Web": "Web",
        "Tại quầy": "At counter",
        "Hệ thống Web": "Web system",
        "Khách lẻ": "Walk-in customer",
        "Khách hàng mẫu": "Sample customer",
        "Quản lý mẫu": "Sample manager",
        "Nhân viên phụ trách": "Assigned staff",
        "Thu ngân": "Cashier",
        "Pha chế": "Barista",
        "Kho": "Warehouse",
        "Mã đơn": "Order code",
        "Mã hóa đơn": "Invoice code",
        "Nhập mã hóa đơn": "Enter invoice code",
        "Ngày tạo": "Created date",
        "Xuất danh sách": "Export list",
        "Xuất báo cáo": "Export report",
        "Mã KH": "Customer ID",
        "Mã NV": "Staff ID",
        "Mã NCC": "Supplier ID",
        "Mã KM": "Promotion ID",
        "Mã DM": "Category ID",
        "Tên khách hàng": "Customer name",
        "Tên NCC": "Supplier name",
        "Người liên hệ": "Contact person",
        "Tên liên hệ": "Contact name",
        "Số sản phẩm": "Product count",
        "Tổng khách hàng": "Total customers",
        "Tổng điểm thưởng": "Total reward points",
        "Điểm trung bình": "Average points",
        "Tất cả hồ sơ": "All profiles",
        "Từ điểm thưởng": "From reward points",
        "Điểm/khách": "Points/customer",
        "Điểm tích lũy": "Reward points",
        "Sử dụng điểm": "Use points",
        "Dùng điểm tích lũy giảm giá": "Use reward points for discount",
        "Khuyến mãi/Giảm giá": "Promotion/Discount",
        "Tổng danh mục": "Total categories",
        "Danh mục có hàng": "Categories with products",
        "Có sản phẩm liên kết": "Has linked products",
        "Đang có trong hệ thống": "Currently in the system",
        "Cập nhật": "Update",
        "Cập nhật tồn kho": "Update stock",
        "Mã sản phẩm": "Product code",
        "Đơn vị tính": "Unit",
        "Link hình ảnh": "Image link",
        "Nhập mô tả ngắn...": "Enter a short description...",
        "Ví dụ DR001": "Example DR001",
        "cái": "piece",
        "Khách có thể tìm thấy sản phẩm và thêm vào giỏ hàng khi trạng thái này được bật.": "Customers can find the product and add it to their cart when this status is enabled.",
        "Mì gói, sữa, đồ ăn nhanh": "Instant noodles, milk, fast food",
        "Sản phẩm tiện lợi MiniStop": "MiniStop convenience products",
        "Nước ngọt, nước suối, cà phê": "Soft drinks, water, coffee",
        "Snack, kẹo, bánh": "Snacks, candy, cakes",
        "Vệ sinh, hộp đựng": "Cleaning and storage items",
        "Mã áp dụng": "Promo code",
        "Tên chương trình": "Program name",
        "Chương trình": "Program",
        "Tổng chương trình": "Total programs",
        "Trong 7 ngày": "Within 7 days",
        "Qua ngày kết thúc": "Past end date",
        "Tất cả khuyến mãi": "All promotions",
        "Khuyến mãi cuối tuần": "Weekend promotion",
        "Không tìm thấy sản phẩm phù hợp.": "No matching products found.",
        "Vui lòng thêm sản phẩm vào hóa đơn.": "Please add products to the invoice.",
        "Hóa đơn mới": "New invoice",
        "Thanh toán ngay": "Pay now",
        "Tìm kiếm hoặc quét mã sản phẩm...": "Search or scan product code...",
        "Chưa có sản phẩm nào": "No products yet",
        "Chưa có sản phẩm nào trong hóa đơn.": "No products in the invoice yet."
    }));

    const tuDienDich = new Map();
    dictionary.forEach((en, vi) => {
        tuDienDich.set(vi, en);
        tuDienDich.set(suaChuBiLoiBangMaHoa(vi), en);
    });
    boSungTuDienChuan.forEach((en, vi) => {
        tuDienDich.set(vi, en);
        tuDienDich.set(suaChuBiLoiBangMaHoa(vi), en);
    });

    const placeholders = new Map(Object.entries({
        "Tìm đồ uống, bánh kẹo, mì gói...": "Search drinks, snacks, instant noodles...",
        "Nhập tên sản phẩm": "Enter product name",
        "example@ministop.vn": "example@ministop.vn",
        "Tối thiểu 6 ký tự": "At least 6 characters",
        "Nguyễn Văn A": "John Smith",
        "0909 123 456": "0909 123 456",
        "Nhập lại mật khẩu": "Re-enter password",
        "Nhập hoặc quét mã sản phẩm": "Enter or scan product code",
        "Tìm theo mã hoặc tên sản phẩm": "Search by code or product name"
    }));

    const ignoredTags = new Set(["SCRIPT", "STYLE", "NOSCRIPT", "TEXTAREA"]);
    const originalTextNodes = new WeakMap();

    function chuanHoaNgonNgu(language) {
        return language === "en" ? "en" : defaultLanguage;
    }

    function getLanguage() {
        return chuanHoaNgonNgu(localStorage.getItem(storageKey));
    }

    function setLanguage(language, clickedButton) {
        const ngonNgu = chuanHoaNgonNgu(language);
        localStorage.setItem(storageKey, ngonNgu);
        document.documentElement.lang = ngonNgu === "en" ? "en" : "vi";
        updateButtons(ngonNgu, clickedButton);
        translatePage();
        window.requestAnimationFrame(() => updateButtons(ngonNgu, clickedButton));
    }

    function translateValue(value, language) {
        if (language !== "en") return value;

        const trimmed = value.trim();
        if (!trimmed) return value;

        if (tuDienDich.has(trimmed)) {
            return value.replace(trimmed, tuDienDich.get(trimmed));
        }

        let translated = value;
        const entries = Array.from(tuDienDich.entries()).sort((a, b) => b[0].length - a[0].length);
        for (const [vi, en] of entries) {
            if (translated.includes(vi)) {
                translated = translated.split(vi).join(en);
            }
        }

        translated = translated
            .replace(/\b(\d+)\s*sản phẩm\b/g, "$1 products")
            .replace(/\bCòn\s+(\d+)/g, "Stock $1")
            .replace(/\bTối đa\s+(\d+)/g, "Maximum $1")
            .replace(/\bcho\s+"([^"]+)"/g, "for \"$1\"")
            .replace(/\b(\d+)\s*sản phẩm\b/g, "$1 products")
            .replace(/\bCòn\s+(\d+)/g, "Stock $1")
            .replace(/\bTối đa\s+(\d+)/g, "Maximum $1")
            .replace(/\bcho\s+"([^"]+)"/g, "for \"$1\"")
            .replace(/\b(\d+)\s*sản phẩm\b/g, "$1 products")
            .replace(/\bCòn\s+(\d+)/g, "Stock $1")
            .replace(/\bTối đa\s+(\d+)/g, "Maximum $1")
            .replace(/\b(\d+)\s*điểm tích lũy\b/g, "$1 reward points")
            .replace(/\b(\d+)\s*điểm\b/g, "$1 points")
            .replace(/\+(\d[\d.,]*)\s*điểm tích lũy\b/g, "+$1 reward points")
            .replace(/\+(\d[\d.,]*)\s*điểm\b/g, "+$1 points")
            .replace(/\bĐiểm tích lũy:\s*/g, "Reward points: ")
            .replace(/\bĐơn này cộng\s*/g, "This order adds ")
            .replace(/\bKH:\s*/g, "Customer: ")
            .replace(/\s-\sĐiểm:\s/g, " - Points: ")
            .replace(/\(Đã trừ\s+([^)]+?)\s+từ điểm\)/g, "(Deducted $1 from points)")
            .replace(/\bKênh:\s*/g, "Channel: ")
            .replace(/\bNhân viên phụ trách:\s*/g, "Assigned staff: ");

        return translated;
    }

    function translateTextNode(node, language) {
        if (!node.parentElement || ignoredTags.has(node.parentElement.tagName)) return;

        if (!originalTextNodes.has(node)) {
            originalTextNodes.set(node, node.nodeValue);
        }

        const original = originalTextNodes.get(node);
        node.nodeValue = translateValue(original, language);
    }

    function translateAttributes(element, language) {
        for (const attr of ["placeholder", "aria-label", "title"]) {
            if (!element.hasAttribute(attr)) continue;
            const dataName = `i18nOriginal${attr}`;
            if (!element.dataset[dataName]) {
                element.dataset[dataName] = element.getAttribute(attr) || "";
            }
            const original = element.dataset[dataName];
            const translated = attr === "placeholder" && language === "en" && placeholders.has(original)
                ? placeholders.get(original)
                : translateValue(original, language);
            element.setAttribute(attr, translated);
        }
    }

    function walk(root, language) {
        const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
            acceptNode(node) {
                if (!node.nodeValue || !node.nodeValue.trim()) return NodeFilter.FILTER_REJECT;
                if (node.parentElement && ignoredTags.has(node.parentElement.tagName)) return NodeFilter.FILTER_REJECT;
                return NodeFilter.FILTER_ACCEPT;
            }
        });

        const nodes = [];
        while (walker.nextNode()) nodes.push(walker.currentNode);
        nodes.forEach(node => translateTextNode(node, language));

        if (root.querySelectorAll) {
            root.querySelectorAll("[placeholder], [aria-label], [title]").forEach(element => translateAttributes(element, language));
        }
    }

    function translatePage() {
        const language = getLanguage();
        walk(document.body, language);
        updateButtons(language);
    }

    function updateButtons(language = getLanguage(), clickedButton = null) {
        const ngonNgu = chuanHoaNgonNgu(language);
        document.querySelectorAll("[data-language-button]").forEach(button => {
            const active = button === clickedButton || button.dataset.languageButton === ngonNgu;
            button.classList.toggle("is-active", active);
            button.setAttribute("aria-pressed", active ? "true" : "false");
        });
    }

    function initObserver() {
        const observer = new MutationObserver(mutations => {
            const language = getLanguage();
            if (language !== "en") return;

            for (const mutation of mutations) {
                if (mutation.type === "characterData") {
                    translateTextNode(mutation.target, language);
                    continue;
                }

                mutation.addedNodes.forEach(node => {
                    if (node.nodeType === Node.TEXT_NODE) {
                        translateTextNode(node, language);
                    } else if (node.nodeType === Node.ELEMENT_NODE) {
                        walk(node, language);
                    }
                });
            }
            updateButtons();
        });

        observer.observe(document.body, { childList: true, subtree: true, characterData: true });
    }

    function initButtons() {
        document.querySelectorAll("[data-language-button]").forEach(button => {
            button.addEventListener("click", (event) => {
                event.preventDefault();
                setLanguage(button.dataset.languageButton || defaultLanguage, button);
            });
        });
        updateButtons();
    }

    function init() {
        document.documentElement.lang = getLanguage() === "en" ? "en" : "vi";
        initButtons();
        translatePage();
        initObserver();
    }

    return { init, setLanguage, getLanguage };
})();

document.addEventListener("DOMContentLoaded", ministopI18n.init);
window.ministopI18n = ministopI18n;
