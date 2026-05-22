(function () {
    const state = {
        currentStep: "scan",
        booking: null,
        roomPlan: null,
        selectedRooms: new Map(),
        scanner: null,
        scannerRunning: false
    };

    const elements = {
        alertArea: document.getElementById("alertArea"),
        bookingCodeInput: document.getElementById("bookingCodeInput"),
        manualLookupForm: document.getElementById("manualLookupForm"),
        startScannerBtn: document.getElementById("startScannerBtn"),
        stopScannerBtn: document.getElementById("stopScannerBtn"),
        scannerFrame: document.querySelector(".scanner-frame"),
        guestDetails: document.getElementById("guestDetails"),
        bookingDetails: document.getElementById("bookingDetails"),
        roomRequirements: document.getElementById("roomRequirements"),
        continueToRoomsBtn: document.getElementById("continueToRoomsBtn"),
        roomGroups: document.getElementById("roomGroups"),
        continueToConfirmBtn: document.getElementById("continueToConfirmBtn"),
        confirmDetails: document.getElementById("confirmDetails"),
        confirmRooms: document.getElementById("confirmRooms"),
        confirmCheckInBtn: document.getElementById("confirmCheckInBtn"),
        successMessage: document.getElementById("successMessage"),
        assignedRooms: document.getElementById("assignedRooms"),
        newCheckInBtn: document.getElementById("newCheckInBtn")
    };

    elements.manualLookupForm.addEventListener("submit", function (event) {
        event.preventDefault();
        lookupBooking(elements.bookingCodeInput.value);
    });

    elements.startScannerBtn.addEventListener("click", startScanner);
    elements.stopScannerBtn.addEventListener("click", stopScanner);
    elements.continueToRoomsBtn.addEventListener("click", loadRooms);
    elements.continueToConfirmBtn.addEventListener("click", showConfirmStep);
    elements.confirmCheckInBtn.addEventListener("click", confirmCheckIn);
    elements.newCheckInBtn.addEventListener("click", resetFlow);

    document.querySelectorAll("[data-back-step]").forEach(function (button) {
        button.addEventListener("click", function () {
            setStep(button.dataset.backStep);
        });
    });

    function setStep(step) {
        state.currentStep = step;
        document.querySelectorAll("[data-step-panel]").forEach(function (panel) {
            panel.classList.toggle("active", panel.dataset.stepPanel === step);
        });

        const order = ["scan", "details", "rooms", "confirm", "done"];
        const activeIndex = order.indexOf(step);
        document.querySelectorAll("[data-step-indicator]").forEach(function (indicator) {
            const index = order.indexOf(indicator.dataset.stepIndicator);
            indicator.classList.toggle("active", index <= activeIndex);
        });

        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    async function lookupBooking(input) {
        const code = (input || "").trim();
        if (!code) {
            showAlert("Nhập mã đặt phòng hoặc quét QR trước.", "warning");
            return;
        }

        setBusy(elements.manualLookupForm, true);
        showAlert("Đang kiểm tra đặt phòng...", "info");
        try {
            const result = await fetchJson(`/Test/ReceptionistLookup?code=${encodeURIComponent(code)}`);
            if (!result.success || !result.booking) {
                showAlert(result.message || "Không tìm thấy đặt phòng.", "danger");
                return;
            }

            state.booking = result.booking;
            state.roomPlan = null;
            state.selectedRooms.clear();
            renderBooking(result.booking);
            showAlert(result.message, result.booking.canCheckIn ? "success" : "warning");
            setStep("details");
        } catch (error) {
            showAlert(error.message, "danger");
        } finally {
            setBusy(elements.manualLookupForm, false);
        }
    }

    function renderBooking(booking) {
        elements.guestDetails.innerHTML = [
            detailRow("Họ tên", booking.customerName),
            detailRow("Số điện thoại", booking.phoneNumber || "Chưa có"),
            detailRow("Email", booking.email || "Chưa có"),
            detailRow("CCCD", booking.identityNumber || "Chưa có")
        ].join("");

        elements.bookingDetails.innerHTML = [
            detailRow("Mã đặt phòng", booking.bookingCode),
            detailRow("Ngày nhận phòng", booking.checkInDate),
            detailRow("Ngày trả phòng", booking.checkOutDate),
            detailRow("Số đêm", `${booking.nights}`),
            detailRow("Tổng thanh toán", formatMoney(booking.totalAmount)),
            detailRowHtml("Đã thanh toán", `<span class="paid-chip"><i class="bi bi-check-circle-fill"></i>${formatMoney(booking.paidAmount)}</span>`),
            detailRow("Trạng thái booking", booking.bookingStatus),
            detailRow("Trạng thái hóa đơn", booking.invoiceStatus)
        ].join("");

        elements.roomRequirements.innerHTML = booking.requirements.map(function (item) {
            return `
                <div class="requirement-item">
                    <strong>${escapeHtml(item.roomTypeName)} <small>(${escapeHtml(item.roomTypeId)})</small></strong>
                    <span>${item.requiredRooms} phòng - ${item.guests} khách</span>
                </div>
            `;
        }).join("");

        elements.continueToRoomsBtn.disabled = !booking.canCheckIn;
    }

    async function loadRooms() {
        if (!state.booking || !state.booking.canCheckIn) {
            showAlert("Đặt phòng này chưa đủ điều kiện check-in.", "warning");
            return;
        }

        elements.continueToRoomsBtn.disabled = true;
        showAlert("Đang tải sơ đồ phòng...", "info");
        try {
            const result = await fetchJson(`/Test/ReceptionistRooms?bookingCode=${encodeURIComponent(state.booking.bookingCode)}`);
            if (!result.success) {
                showAlert(result.message || "Không tải được sơ đồ phòng.", "danger");
                return;
            }

            state.roomPlan = result;
            state.selectedRooms.clear();
            renderRoomGroups(result.groups || []);
            updateRoomSelectionState();
            showAlert("Chọn đúng số phòng theo từng loại phòng khách đã đặt.", "success");
            setStep("rooms");
        } catch (error) {
            showAlert(error.message, "danger");
        } finally {
            elements.continueToRoomsBtn.disabled = false;
        }
    }

    function renderRoomGroups(groups) {
        elements.roomGroups.innerHTML = groups.map(function (group) {
            const rooms = group.rooms.map(function (room) {
                const disabled = room.isSelectable ? "" : "disabled";
                return `
                    <button type="button"
                            class="room-tile ${escapeHtml(room.status)}"
                            data-room-id="${escapeHtml(room.roomId)}"
                            data-room-type-id="${escapeHtml(room.roomTypeId)}"
                            ${disabled}
                            aria-pressed="false">
                        <strong>${escapeHtml(room.roomNumber)}</strong>
                        <small>Tầng ${room.floor}</small>
                        <small>${escapeHtml(room.roomTypeName)}</small>
                        <small>${escapeHtml(room.statusLabel)}</small>
                    </button>
                `;
            }).join("");

            return `
                <section class="room-type-section" data-room-type-section="${escapeHtml(group.roomTypeId)}" data-required="${group.requiredRooms}">
                    <div class="room-type-header">
                        <strong>${escapeHtml(group.roomTypeName)} <small>(${escapeHtml(group.roomTypeId)})</small></strong>
                        <span class="room-counter" data-room-counter="${escapeHtml(group.roomTypeId)}">0/${group.requiredRooms} đã chọn</span>
                    </div>
                    <div class="room-grid">${rooms || "<p>Không có phòng thuộc loại này.</p>"}</div>
                </section>
            `;
        }).join("");

        elements.roomGroups.querySelectorAll(".room-tile.available").forEach(function (tile) {
            tile.addEventListener("click", function () {
                toggleRoom(tile);
            });
        });
    }

    function toggleRoom(tile) {
        const roomId = tile.dataset.roomId;
        const roomTypeId = tile.dataset.roomTypeId;
        const group = getRoomGroup(roomTypeId);
        if (!group) {
            return;
        }

        if (state.selectedRooms.has(roomId)) {
            state.selectedRooms.delete(roomId);
            updateRoomSelectionState();
            return;
        }

        const selectedForType = countSelectedForType(roomTypeId);
        if (selectedForType >= group.requiredRooms) {
            showAlert(`Loại phòng ${group.roomTypeName} chỉ cần chọn ${group.requiredRooms} phòng.`, "warning");
            return;
        }

        const room = group.rooms.find(function (item) {
            return item.roomId === roomId;
        });
        if (room) {
            state.selectedRooms.set(roomId, room);
        }
        updateRoomSelectionState();
    }

    function updateRoomSelectionState() {
        elements.roomGroups.querySelectorAll(".room-tile").forEach(function (tile) {
            const selected = state.selectedRooms.has(tile.dataset.roomId);
            tile.classList.toggle("selected", selected);
            tile.setAttribute("aria-pressed", selected ? "true" : "false");
        });

        (state.roomPlan?.groups || []).forEach(function (group) {
            const count = countSelectedForType(group.roomTypeId);
            const counter = elements.roomGroups.querySelector(`[data-room-counter="${cssEscape(group.roomTypeId)}"]`);
            if (counter) {
                counter.textContent = `${count}/${group.requiredRooms} đã chọn`;
            }
        });

        elements.continueToConfirmBtn.disabled = !hasValidRoomSelection();
    }

    function showConfirmStep() {
        if (!hasValidRoomSelection()) {
            showAlert("Chọn đủ phòng theo từng loại trước khi xác nhận.", "warning");
            return;
        }

        elements.confirmDetails.innerHTML = [
            confirmRow("Mã đặt phòng", state.booking.bookingCode),
            confirmRow("Khách hàng", state.booking.customerName),
            confirmRow("Số điện thoại", state.booking.phoneNumber || "Chưa có"),
            confirmRow("Ngày ở", `${state.booking.checkInDate} - ${state.booking.checkOutDate}`),
            confirmRow("Đã thanh toán", formatMoney(state.booking.paidAmount))
        ].join("");

        elements.confirmRooms.innerHTML = renderSelectedRooms();
        showAlert("Kiểm tra lại thông tin trước khi giao phòng cho khách.", "info");
        setStep("confirm");
    }

    async function confirmCheckIn() {
        if (!state.booking || !hasValidRoomSelection()) {
            showAlert("Dữ liệu check-in chưa hợp lệ.", "warning");
            return;
        }

        setButtonBusy(elements.confirmCheckInBtn, true, "Đang check-in...");
        try {
            const result = await fetchJson("/Test/ReceptionistCheckIn", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    bookingCode: state.booking.bookingCode,
                    roomIds: Array.from(state.selectedRooms.keys())
                })
            });

            if (!result.success) {
                showAlert(result.message || "Check-in thất bại.", "danger");
                return;
            }

            elements.successMessage.textContent = result.message || "Khách đã được nhận phòng.";
            elements.assignedRooms.innerHTML = (result.assignedRooms || []).map(function (room) {
                return `
                    <div class="selected-room">
                        <strong>Phòng ${escapeHtml(room.roomNumber)}</strong>
                        <span>${escapeHtml(room.roomTypeName)} (${escapeHtml(room.roomTypeId)})</span>
                    </div>
                `;
            }).join("");
            showAlert("Check-in đã được lưu vào hệ thống.", "success");
            setStep("done");
        } catch (error) {
            showAlert(error.message, "danger");
        } finally {
            setButtonBusy(elements.confirmCheckInBtn, false);
        }
    }

    async function startScanner() {
        if (!window.Html5Qrcode) {
            showAlert("Không tải được thư viện quét QR. Bạn vẫn có thể nhập mã thủ công.", "warning");
            return;
        }

        if (!state.scanner) {
            state.scanner = new Html5Qrcode("qrReader");
        }

        if (state.scannerRunning) {
            return;
        }

        try {
            await state.scanner.start(
                { facingMode: "environment" },
                { fps: 10, qrbox: { width: 240, height: 240 } },
                async function (decodedText) {
                    await stopScanner();
                    elements.bookingCodeInput.value = decodedText;
                    lookupBooking(decodedText);
                });
            state.scannerRunning = true;
            elements.scannerFrame.classList.add("is-active");
            elements.startScannerBtn.disabled = true;
            elements.stopScannerBtn.disabled = false;
        } catch (error) {
            showAlert("Không mở được camera. Kiểm tra quyền camera hoặc nhập mã thủ công.", "warning");
        }
    }

    async function stopScanner() {
        if (!state.scanner || !state.scannerRunning) {
            return;
        }

        try {
            await state.scanner.stop();
        } finally {
            state.scannerRunning = false;
            elements.scannerFrame.classList.remove("is-active");
            elements.startScannerBtn.disabled = false;
            elements.stopScannerBtn.disabled = true;
        }
    }

    function resetFlow() {
        stopScanner();
        state.booking = null;
        state.roomPlan = null;
        state.selectedRooms.clear();
        elements.bookingCodeInput.value = "";
        elements.guestDetails.innerHTML = "";
        elements.bookingDetails.innerHTML = "";
        elements.roomRequirements.innerHTML = "";
        elements.roomGroups.innerHTML = "";
        elements.confirmRooms.innerHTML = "";
        elements.assignedRooms.innerHTML = "";
        showAlert("Sẵn sàng check-in lượt mới.", "info");
        setStep("scan");
    }

    function getRoomGroup(roomTypeId) {
        return (state.roomPlan?.groups || []).find(function (group) {
            return group.roomTypeId === roomTypeId;
        });
    }

    function countSelectedForType(roomTypeId) {
        return Array.from(state.selectedRooms.values()).filter(function (room) {
            return room.roomTypeId === roomTypeId;
        }).length;
    }

    function hasValidRoomSelection() {
        if (!state.roomPlan) {
            return false;
        }

        return state.roomPlan.groups.every(function (group) {
            return countSelectedForType(group.roomTypeId) === group.requiredRooms;
        });
    }

    function renderSelectedRooms() {
        return Array.from(state.selectedRooms.values())
            .sort(function (a, b) {
                return a.roomNumber.localeCompare(b.roomNumber);
            })
            .map(function (room) {
                return `
                    <div class="selected-room">
                        <strong>Phòng ${escapeHtml(room.roomNumber)}</strong>
                        <span>${escapeHtml(room.roomTypeName)} (${escapeHtml(room.roomTypeId)}) - Tầng ${room.floor}</span>
                    </div>
                `;
            }).join("");
    }

    async function fetchJson(url, options) {
        const response = await fetch(url, options);
        if (!response.ok) {
            throw new Error("Không thể kết nối máy chủ. Vui lòng thử lại.");
        }

        return response.json();
    }

    function setBusy(container, isBusy) {
        container.querySelectorAll("button, input").forEach(function (item) {
            item.disabled = isBusy;
        });
    }

    function setButtonBusy(button, isBusy, label) {
        if (isBusy) {
            button.dataset.originalHtml = button.innerHTML;
            button.disabled = true;
            button.innerHTML = `<span class="spinner-border spinner-border-sm" aria-hidden="true"></span> ${label}`;
        } else {
            button.disabled = false;
            if (button.dataset.originalHtml) {
                button.innerHTML = button.dataset.originalHtml;
            }
        }
    }

    function detailRow(label, value) {
        return `<div class="detail-row"><span>${escapeHtml(label)}</span><strong>${escapeHtml(value)}</strong></div>`;
    }

    function detailRowHtml(label, value) {
        return `<div class="detail-row"><span>${escapeHtml(label)}</span><strong>${value}</strong></div>`;
    }

    function confirmRow(label, value) {
        return `<div class="confirm-row"><span>${escapeHtml(label)}</span><strong>${escapeHtml(value)}</strong></div>`;
    }

    function showAlert(message, type) {
        elements.alertArea.innerHTML = `
            <div class="alert alert-${type} alert-dismissible fade show" role="alert">
                ${escapeHtml(message)}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>
        `;
    }

    function formatMoney(value) {
        return new Intl.NumberFormat("vi-VN", {
            style: "currency",
            currency: "VND",
            maximumFractionDigits: 0
        }).format(value || 0);
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function cssEscape(value) {
        if (window.CSS && window.CSS.escape) {
            return window.CSS.escape(value);
        }

        return String(value).replace(/["\\]/g, "\\$&");
    }
})();
