(function () {
    const state = {
        currentStep: "scan",
        lastWorkflowStep: "scan",
        booking: null,
        roomPlan: null,
        roomMap: null,
        roomMapFilter: "all",
        serviceUsage: null,
        checkoutList: null,
        walkInAvailability: null,
        selectedWalkInRooms: new Map(),
        selectedCheckoutCode: "",
        selectedRooms: new Map(),
        scanner: null,
        scannerRunning: false
    };

    const elements = {
        alertArea: document.getElementById("alertArea"),
        workflowStrip: document.querySelector(".workflow-strip"),
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
        newCheckInBtn: document.getElementById("newCheckInBtn"),
        refreshCheckInBtn: document.getElementById("refreshCheckInBtn"),
        checkInNavBtn: document.getElementById("checkInNavBtn"),
        walkInNavBtn: document.getElementById("walkInNavBtn"),
        todayArrivalsNavBtn: document.getElementById("todayArrivalsNavBtn"),
        roomMapNavBtn: document.getElementById("roomMapNavBtn"),
        serviceUsageNavBtn: document.getElementById("serviceUsageNavBtn"),
        checkoutNavBtn: document.getElementById("checkoutNavBtn"),
        todayArrivalsSummary: document.getElementById("todayArrivalsSummary"),
        todayArrivalsList: document.getElementById("todayArrivalsList"),
        roomMapRefreshBtn: document.getElementById("roomMapRefreshBtn"),
        roomMapSummary: document.getElementById("roomMapSummary"),
        roomMapFloors: document.getElementById("roomMapFloors"),
        serviceUsageRefreshBtn: document.getElementById("serviceUsageRefreshBtn"),
        serviceUsageSummary: document.getElementById("serviceUsageSummary"),
        serviceStayList: document.getElementById("serviceStayList"),
        serviceUsageForm: document.getElementById("serviceUsageForm"),
        serviceBookingSelect: document.getElementById("serviceBookingSelect"),
        serviceOptionSelect: document.getElementById("serviceOptionSelect"),
        serviceQuantityInput: document.getElementById("serviceQuantityInput"),
        serviceNoteInput: document.getElementById("serviceNoteInput"),
        serviceLineEstimate: document.getElementById("serviceLineEstimate"),
        addServiceUsageBtn: document.getElementById("addServiceUsageBtn"),
        checkoutRefreshBtn: document.getElementById("checkoutRefreshBtn"),
        checkoutSummary: document.getElementById("checkoutSummary"),
        checkoutStayList: document.getElementById("checkoutStayList"),
        checkoutDetail: document.getElementById("checkoutDetail"),
        checkoutPaymentForm: document.getElementById("checkoutPaymentForm"),
        checkoutBookingCode: document.getElementById("checkoutBookingCode"),
        checkoutPaymentAmount: document.getElementById("checkoutPaymentAmount"),
        checkoutPaymentMethod: document.getElementById("checkoutPaymentMethod"),
        checkoutNote: document.getElementById("checkoutNote"),
        confirmCheckoutBtn: document.getElementById("confirmCheckoutBtn"),
        walkInResetBtn: document.getElementById("walkInResetBtn"),
        walkInForm: document.getElementById("walkInForm"),
        walkInCustomerName: document.getElementById("walkInCustomerName"),
        walkInPhone: document.getElementById("walkInPhone"),
        walkInIdentity: document.getElementById("walkInIdentity"),
        walkInEmail: document.getElementById("walkInEmail"),
        walkInGuestCount: document.getElementById("walkInGuestCount"),
        walkInCheckoutDate: document.getElementById("walkInCheckoutDate"),
        walkInGender: document.getElementById("walkInGender"),
        walkInNationality: document.getElementById("walkInNationality"),
        walkInAddress: document.getElementById("walkInAddress"),
        walkInNote: document.getElementById("walkInNote"),
        walkInLoadRoomsBtn: document.getElementById("walkInLoadRoomsBtn"),
        walkInRoomSummary: document.getElementById("walkInRoomSummary"),
        walkInRoomGroups: document.getElementById("walkInRoomGroups"),
        walkInTotalAmount: document.getElementById("walkInTotalAmount"),
        walkInPaymentAmount: document.getElementById("walkInPaymentAmount"),
        walkInPaymentMethod: document.getElementById("walkInPaymentMethod"),
        walkInConfirmBtn: document.getElementById("walkInConfirmBtn"),
        walkInGuestSummary: document.getElementById("walkInGuestSummary"),
        walkInEditGuestBtn: document.getElementById("walkInEditGuestBtn"),
        walkInViews: document.querySelectorAll("[data-walkin-view]")
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
    elements.refreshCheckInBtn?.addEventListener("click", resetFlow);
    elements.walkInNavBtn?.addEventListener("click", showWalkIn);
    elements.walkInForm?.addEventListener("submit", function (event) {
        event.preventDefault();
        loadWalkInAvailability();
    });
    elements.walkInResetBtn?.addEventListener("click", resetWalkIn);
    elements.walkInEditGuestBtn?.addEventListener("click", function () {
        showWalkInView("intake");
    });
    elements.walkInConfirmBtn?.addEventListener("click", submitWalkInCheckIn);
    elements.walkInPaymentAmount?.addEventListener("input", updateWalkInSelectionState);
    elements.walkInCheckoutDate?.addEventListener("change", function () {
        state.walkInAvailability = null;
        state.selectedWalkInRooms.clear();
        renderWalkInAvailability();
    });
    elements.checkInNavBtn?.addEventListener("click", function () {
        const isToolStep = state.currentStep === "today" ||
            state.currentStep === "roomMap" ||
            state.currentStep === "services" ||
            state.currentStep === "checkout" ||
            state.currentStep === "walkIn";
        setStep(isToolStep ? state.lastWorkflowStep : state.currentStep);
    });
    elements.todayArrivalsNavBtn?.addEventListener("click", loadTodayArrivals);
    elements.roomMapNavBtn?.addEventListener("click", loadRoomMap);
    elements.roomMapRefreshBtn?.addEventListener("click", loadRoomMap);
    elements.serviceUsageNavBtn?.addEventListener("click", loadServiceUsage);
    elements.serviceUsageRefreshBtn?.addEventListener("click", loadServiceUsage);
    elements.serviceUsageForm?.addEventListener("submit", addServiceUsage);
    elements.serviceBookingSelect?.addEventListener("change", renderServiceSelection);
    elements.serviceOptionSelect?.addEventListener("change", updateServiceEstimate);
    elements.serviceQuantityInput?.addEventListener("input", updateServiceEstimate);
    elements.checkoutNavBtn?.addEventListener("click", loadCheckoutList);
    elements.checkoutRefreshBtn?.addEventListener("click", loadCheckoutList);
    elements.checkoutPaymentForm?.addEventListener("submit", submitCheckout);
    document.querySelectorAll("[data-room-map-filter]").forEach(function (button) {
        button.addEventListener("click", function () {
            state.roomMapFilter = button.dataset.roomMapFilter || "all";
            document.querySelectorAll("[data-room-map-filter]").forEach(function (item) {
                item.classList.toggle("active", item === button);
            });
            renderRoomMap();
        });
    });

    document.querySelectorAll("[data-step-indicator]").forEach(function (button) {
        button.addEventListener("click", function () {
            goToStepFromIndicator(button.dataset.stepIndicator);
        });
    });

    document.querySelectorAll("[data-back-step]").forEach(function (button) {
        button.addEventListener("click", function () {
            setStep(button.dataset.backStep);
        });
    });

    function setStep(step) {
        state.currentStep = step;
        const workflowSteps = ["scan", "details", "rooms", "confirm", "done"];
        if (!workflowSteps.includes(step) && step !== "walkIn") {
            elements.workflowStrip?.setAttribute("hidden", "");
        } else {
            elements.workflowStrip?.removeAttribute("hidden");
        }

        if (workflowSteps.includes(step)) {
            state.lastWorkflowStep = step;
        }
        document.querySelectorAll("[data-step-panel]").forEach(function (panel) {
            panel.classList.toggle("active", panel.dataset.stepPanel === step);
        });

        const order = workflowSteps;
        const activeIndex = order.indexOf(step);
        document.querySelectorAll("[data-step-indicator]").forEach(function (indicator) {
            const index = order.indexOf(indicator.dataset.stepIndicator);
            indicator.classList.toggle("active", index <= activeIndex);
            indicator.classList.toggle("completed", index < activeIndex);
            indicator.classList.toggle("is-available", canNavigateToStep(indicator.dataset.stepIndicator));
        });
        elements.checkInNavBtn?.classList.toggle("active", step !== "today" && step !== "roomMap" && step !== "services" && step !== "checkout" && step !== "walkIn");
        elements.walkInNavBtn?.classList.toggle("active", step === "walkIn");
        elements.todayArrivalsNavBtn?.classList.toggle("active", step === "today");
        elements.roomMapNavBtn?.classList.toggle("active", step === "roomMap");
        elements.serviceUsageNavBtn?.classList.toggle("active", step === "services");
        elements.checkoutNavBtn?.classList.toggle("active", step === "checkout");

        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    function goToStepFromIndicator(step) {
        if (!canNavigateToStep(step)) {
            showAlert("Bước này chưa có đủ dữ liệu để mở.", "warning");
            return;
        }

        setStep(step);
    }

    function canNavigateToStep(step) {
        if (step === "scan") return true;
        if (step === "today") return true;
        if (step === "roomMap") return true;
        if (step === "services") return true;
        if (step === "checkout") return true;
        if (step === "walkIn") return true;
        if (step === "details") return Boolean(state.booking);
        if (step === "rooms") return Boolean(state.roomPlan);
        if (step === "confirm") return Boolean(state.booking && state.roomPlan && hasValidRoomSelection());
        if (step === "done") return state.currentStep === "done";
        return false;
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
            const result = await fetchJson(`/Receptionist/Lookup?code=${encodeURIComponent(code)}`);
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

    function showWalkIn() {
        setStep("walkIn");
        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);
        if (elements.walkInCheckoutDate && !elements.walkInCheckoutDate.value) {
            elements.walkInCheckoutDate.value = tomorrow.toISOString().slice(0, 10);
        }
        showWalkInView(state.walkInAvailability ? "booking" : "intake");
        renderWalkInGuestSummary();
        renderWalkInAvailability();
    }

    function resetWalkIn() {
        state.walkInAvailability = null;
        state.selectedWalkInRooms.clear();
        elements.walkInForm?.reset();
        if (elements.walkInGuestCount) elements.walkInGuestCount.value = "1";
        if (elements.walkInNationality) elements.walkInNationality.value = "Việt Nam";
        if (elements.walkInPaymentAmount) elements.walkInPaymentAmount.value = "0";
        showWalkInView("intake");
        showWalkIn();
    }

    async function loadWalkInAvailability() {
        if (!validateWalkInGuestInfo()) {
            return;
        }

        const checkoutDate = elements.walkInCheckoutDate?.value || "";
        if (!checkoutDate) {
            showAlert("Chọn ngày trả phòng trước khi tìm phòng.", "warning");
            return;
        }

        setButtonBusy(elements.walkInLoadRoomsBtn, true, "Đang tải...");
        try {
            const result = await fetchJson(`/Receptionist/WalkInAvailability?checkOutDate=${encodeURIComponent(checkoutDate)}`);
            if (!result.success) {
                showAlert(result.message || "Không tải được phòng trống.", "danger");
                return;
            }

            state.walkInAvailability = result;
            state.selectedWalkInRooms.clear();
            renderWalkInGuestSummary();
            renderWalkInAvailability();
            showWalkInView("booking");
            showAlert(result.message, "info");
        } catch (error) {
            showAlert(error.message, "danger");
        } finally {
            setButtonBusy(elements.walkInLoadRoomsBtn, false);
        }
    }

    function showWalkInView(view) {
        elements.walkInViews?.forEach(function (item) {
            item.hidden = item.dataset.walkinView !== view;
        });
    }

    function validateWalkInGuestInfo() {
        if (!elements.walkInCustomerName?.value.trim()) {
            showAlert("Nhập họ tên khách vãng lai trước khi tìm phòng.", "warning");
            elements.walkInCustomerName?.focus();
            return false;
        }

        if (Number(elements.walkInGuestCount?.value || 0) <= 0) {
            showAlert("Số khách phải lớn hơn 0.", "warning");
            elements.walkInGuestCount?.focus();
            return false;
        }

        return true;
    }

    function renderWalkInGuestSummary() {
        if (!elements.walkInGuestSummary) return;

        const customerName = elements.walkInCustomerName?.value.trim() || "Khách vãng lai";
        const phone = elements.walkInPhone?.value.trim() || "Chưa có SĐT";
        const identity = elements.walkInIdentity?.value.trim() || "Chưa có giấy tờ";
        const guestCount = Number(elements.walkInGuestCount?.value || 1);
        const checkoutDate = elements.walkInCheckoutDate?.value || "";
        const checkoutLabel = checkoutDate ? formatDateLabel(checkoutDate) : "Chưa chọn ngày trả";

        elements.walkInGuestSummary.innerHTML = `
            <div>
                <p class="eyebrow">Guest</p>
                <h3>${escapeHtml(customerName)}</h3>
            </div>
            <dl>
                <div><dt>SĐT</dt><dd>${escapeHtml(phone)}</dd></div>
                <div><dt>Giấy tờ</dt><dd>${escapeHtml(identity)}</dd></div>
                <div><dt>Số khách</dt><dd>${guestCount}</dd></div>
                <div><dt>Trả phòng</dt><dd>${escapeHtml(checkoutLabel)}</dd></div>
            </dl>
        `;
    }

    function renderWalkInAvailability() {
        if (!elements.walkInRoomGroups) return;

        if (!state.walkInAvailability) {
            elements.walkInRoomSummary.textContent = "Nhập ngày trả phòng để tải phòng trống.";
            elements.walkInRoomGroups.innerHTML = "";
            updateWalkInSelectionState();
            return;
        }

        elements.walkInRoomSummary.textContent = `${state.walkInAvailability.checkInDate} - ${state.walkInAvailability.checkOutDate} · ${state.walkInAvailability.nights} đêm`;
        const groups = state.walkInAvailability.groups || [];
        if (groups.length === 0) {
            elements.walkInRoomGroups.innerHTML = `<div class="arrival-empty">Không có phòng trống phù hợp.</div>`;
            updateWalkInSelectionState();
            return;
        }

        elements.walkInRoomGroups.innerHTML = groups.map(function (group) {
            const rooms = (group.rooms || []).map(function (room) {
                const status = room.status || "available";
                const disabled = room.isSelectable ? "" : "disabled";
                return `
                    <button type="button"
                            class="room-tile ${escapeHtml(status)}"
                            data-walkin-room-id="${escapeHtml(room.roomId)}"
                            ${disabled}
                            aria-pressed="false">
                        <strong>${escapeHtml(room.roomNumber)}</strong>
                        <small>Tầng ${room.floor}</small>
                        <small>${escapeHtml(room.roomTypeName)}</small>
                        <small>${formatMoney(room.pricePerNight)} / đêm</small>
                        <small>${escapeHtml(room.statusLabel || "")}</small>
                    </button>
                `;
            }).join("");

            const selectableCount = (group.rooms || []).filter(function (room) {
                return room.isSelectable;
            }).length;

            return `
                <section class="room-type-section">
                    <div class="room-type-header">
                        <strong>${escapeHtml(group.roomTypeName)} <small>(${escapeHtml(group.roomTypeId)})</small></strong>
                        <span>${selectableCount}/${group.rooms?.length || 0} phòng có thể chọn</span>
                    </div>
                    <div class="room-grid">${rooms}</div>
                </section>
            `;
        }).join("");

        elements.walkInRoomGroups.querySelectorAll(".room-tile.available[data-walkin-room-id]").forEach(function (tile) {
            tile.addEventListener("click", function () {
                toggleWalkInRoom(tile.dataset.walkinRoomId);
            });
        });
        updateWalkInSelectionState();
    }

    function toggleWalkInRoom(roomId) {
        const room = findWalkInRoom(roomId);
        if (!room || !room.isSelectable) return;

        if (state.selectedWalkInRooms.has(roomId)) {
            state.selectedWalkInRooms.delete(roomId);
        } else {
            state.selectedWalkInRooms.set(roomId, room);
        }

        updateWalkInSelectionState();
    }

    function findWalkInRoom(roomId) {
        const groups = state.walkInAvailability?.groups || [];
        for (const group of groups) {
            const room = (group.rooms || []).find(function (item) {
                return item.roomId === roomId;
            });
            if (room) return room;
        }
        return null;
    }

    function getWalkInTotal() {
        const nights = state.walkInAvailability?.nights || 0;
        return Array.from(state.selectedWalkInRooms.values())
            .reduce(function (total, room) {
                return total + ((room.pricePerNight || 0) * nights);
            }, 0);
    }

    function updateWalkInSelectionState() {
        const total = getWalkInTotal();
        elements.walkInRoomGroups?.querySelectorAll("[data-walkin-room-id]").forEach(function (tile) {
            const selected = state.selectedWalkInRooms.has(tile.dataset.walkinRoomId);
            tile.classList.toggle("selected", selected);
            tile.setAttribute("aria-pressed", selected ? "true" : "false");
        });

        if (elements.walkInTotalAmount) {
            elements.walkInTotalAmount.textContent = formatMoney(total);
        }
        if (elements.walkInPaymentAmount && total > 0 && Number(elements.walkInPaymentAmount.value || 0) < total) {
            elements.walkInPaymentAmount.value = String(total);
        }
        if (elements.walkInConfirmBtn) {
            elements.walkInConfirmBtn.disabled = total <= 0 || Number(elements.walkInPaymentAmount?.value || 0) + 0.01 < total;
        }
    }

    async function submitWalkInCheckIn() {
        const total = getWalkInTotal();
        if (!elements.walkInCustomerName?.value.trim()) {
            showAlert("Nhập họ tên khách vãng lai.", "warning");
            return;
        }
        if (!state.walkInAvailability || state.selectedWalkInRooms.size === 0) {
            showAlert("Chọn ít nhất một phòng trống.", "warning");
            return;
        }
        if (Number(elements.walkInPaymentAmount?.value || 0) + 0.01 < total) {
            showAlert(`Khách cần thanh toán đủ ${formatMoney(total)} trước khi check-in.`, "warning");
            return;
        }

        setButtonBusy(elements.walkInConfirmBtn, true, "Đang check-in...");
        try {
            const result = await fetchJson("/Receptionist/WalkInCheckIn", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    customerName: elements.walkInCustomerName.value,
                    phoneNumber: elements.walkInPhone?.value || "",
                    identityNumber: elements.walkInIdentity?.value || "",
                    email: elements.walkInEmail?.value || "",
                    gender: elements.walkInGender?.value || "",
                    nationality: elements.walkInNationality?.value || "",
                    address: elements.walkInAddress?.value || "",
                    checkOutDate: elements.walkInCheckoutDate?.value || "",
                    guestCount: Number(elements.walkInGuestCount?.value || 1),
                    roomIds: Array.from(state.selectedWalkInRooms.keys()),
                    paymentAmount: Number(elements.walkInPaymentAmount?.value || 0),
                    paymentMethod: elements.walkInPaymentMethod?.value || "",
                    note: elements.walkInNote?.value || ""
                })
            });

            if (!result.success) {
                showAlert(result.message || "Check-in vãng lai thất bại.", "danger");
                return;
            }

            showAlert(`${result.message} Mã đặt phòng ${result.bookingCode}, đã thu ${formatMoney(result.grandTotal)}.`, "success");
            resetWalkIn();
        } catch (error) {
            showAlert(error.message, "danger");
        } finally {
            setButtonBusy(elements.walkInConfirmBtn, false);
        }
    }

    async function loadTodayArrivals() {
        setStep("today");
        if (elements.todayArrivalsSummary) {
            elements.todayArrivalsSummary.textContent = "Đang tải...";
        }
        if (elements.todayArrivalsList) {
            elements.todayArrivalsList.innerHTML = `<div class="arrival-empty">Đang tải danh sách khách đến hôm nay...</div>`;
        }

        try {
            const result = await fetchJson("/Receptionist/Today");
            if (!result.success) {
                showAlert(result.message || "Không tải được danh sách khách đến hôm nay.", "danger");
                return;
            }

            renderTodayArrivals(result);
            showAlert(result.message, "info");
        } catch (error) {
            showAlert(error.message, "danger");
        }
    }

    function renderTodayArrivals(result) {
        const bookings = result.bookings || [];
        if (elements.todayArrivalsSummary) {
            elements.todayArrivalsSummary.textContent = `${bookings.length} booking - ${escapeHtml(result.dateLabel || "")}`;
        }

        if (!elements.todayArrivalsList) return;
        if (bookings.length === 0) {
            elements.todayArrivalsList.innerHTML = `<div class="arrival-empty">Hôm nay chưa có khách dự kiến nhận phòng.</div>`;
            return;
        }

        elements.todayArrivalsList.innerHTML = bookings.map(function (booking) {
            const rooms = (booking.requirements || [])
                .map(function (item) {
                    return `${item.requiredRooms} ${escapeHtml(item.roomTypeName)}`;
                })
                .join(" · ");
            const statusClass = booking.canCheckIn ? "ready" : "blocked";
            const buttonText = booking.canCheckIn ? "Check-in" : "Xem chi tiết";

            return `
                <article class="arrival-card ${statusClass}" data-arrival-code="${escapeHtml(booking.bookingCode)}">
                    <div class="arrival-main">
                        <div class="arrival-code">${escapeHtml(booking.bookingCode)}</div>
                        <h3>${escapeHtml(booking.customerName)}</h3>
                        <p>${escapeHtml(booking.phoneNumber || "Chưa có số điện thoại")}</p>
                        <div class="arrival-meta">
                            <span><i class="bi bi-calendar-check"></i>${escapeHtml(booking.checkInDate)} - ${escapeHtml(booking.checkOutDate)}</span>
                            <span><i class="bi bi-door-open"></i>${rooms || "Chưa có dòng phòng"}</span>
                        </div>
                    </div>
                    <div class="arrival-side">
                        <span class="arrival-status">${escapeHtml(booking.canCheckIn ? "Sẵn sàng" : booking.checkInMessage)}</span>
                        <strong>${formatMoney(booking.paidAmount)}</strong>
                        <button type="button" class="btn btn-primary" data-arrival-action="${escapeHtml(booking.bookingCode)}">
                            ${buttonText}
                        </button>
                    </div>
                </article>
            `;
        }).join("");

        elements.todayArrivalsList.querySelectorAll("[data-arrival-action]").forEach(function (button) {
            button.addEventListener("click", function () {
                lookupBooking(button.dataset.arrivalAction);
            });
        });
    }

    async function loadRoomMap() {
        setStep("roomMap");
        if (elements.roomMapSummary) {
            elements.roomMapSummary.innerHTML = `<span>Đang tải sơ đồ phòng...</span>`;
        }
        if (elements.roomMapFloors) {
            elements.roomMapFloors.innerHTML = `<div class="arrival-empty">Đang tải danh sách phòng...</div>`;
        }

        try {
            const result = await fetchJson("/Receptionist/RoomMap");
            if (!result.success) {
                showAlert(result.message || "Không tải được sơ đồ phòng.", "danger");
                return;
            }

            state.roomMap = result;
            renderRoomMap();
            showAlert(result.message, "info");
        } catch (error) {
            showAlert(error.message, "danger");
        }
    }

    function renderRoomMap() {
        if (!state.roomMap) return;

        const counts = state.roomMap.statusCounts || [];
        if (elements.roomMapSummary) {
            elements.roomMapSummary.innerHTML = counts.map(function (item) {
                return `<span class="room-map-count ${escapeHtml(item.status)}">${escapeHtml(item.statusLabel)}: <strong>${item.count}</strong></span>`;
            }).join("");
        }

        const floors = state.roomMap.floors || [];
        if (!elements.roomMapFloors) return;
        if (floors.length === 0) {
            elements.roomMapFloors.innerHTML = `<div class="arrival-empty">Chưa có phòng trong hệ thống.</div>`;
            return;
        }

        const filter = state.roomMapFilter || "all";
        const html = floors.map(function (floor) {
            const rooms = (floor.rooms || []).filter(function (room) {
                return filter === "all" || room.status === filter;
            });

            if (rooms.length === 0) return "";

            return `
                <section class="room-map-floor">
                    <div class="room-map-floor-header">
                        <h3>Tầng ${floor.floor}</h3>
                        <span>${rooms.length} phòng</span>
                    </div>
                    <div class="room-map-grid">
                        ${rooms.map(renderRoomMapTile).join("")}
                    </div>
                </section>
            `;
        }).join("");

        elements.roomMapFloors.innerHTML = html || `<div class="arrival-empty">Không có phòng phù hợp bộ lọc hiện tại.</div>`;
    }

    function renderRoomMapTile(room) {
        const note = room.status === "occupied" && room.bookingCode
            ? `<span>${escapeHtml(room.currentGuestName || "Khách đang ở")}</span><span>${escapeHtml(room.bookingCode)} · Trả ${escapeHtml(room.checkOutDate || "")}</span>`
            : `<span>${escapeHtml(room.roomTypeName)}</span><span>${escapeHtml(room.statusLabel)}</span>`;

        return `
            <article class="room-map-tile ${escapeHtml(room.status)}">
                <div>
                    <strong>${escapeHtml(room.roomNumber)}</strong>
                    <small>${escapeHtml(room.roomTypeName)}</small>
                </div>
                <div class="room-map-tile-meta">${note}</div>
            </article>
        `;
    }

    async function loadServiceUsage(preferredBookingCode) {
        const preferredCode = typeof preferredBookingCode === "string"
            ? preferredBookingCode
            : elements.serviceBookingSelect?.value;

        setStep("services");
        if (elements.serviceUsageSummary) {
            elements.serviceUsageSummary.textContent = "Đang tải...";
        }
        if (elements.serviceStayList) {
            elements.serviceStayList.innerHTML = `<div class="arrival-empty">Đang tải danh sách khách đang lưu trú...</div>`;
        }

        try {
            const result = await fetchJson("/Receptionist/ServiceUsage");
            if (!result.success) {
                showAlert(result.message || "Không tải được dữ liệu dịch vụ.", "danger");
                return;
            }

            state.serviceUsage = result;
            renderServiceUsage(preferredCode);
            showAlert(result.message, "info");
        } catch (error) {
            showAlert(error.message, "danger");
        }
    }

    function renderServiceUsage(preferredBookingCode) {
        const activeStays = state.serviceUsage?.activeStays || [];
        const services = state.serviceUsage?.services || [];

        if (elements.serviceUsageSummary) {
            elements.serviceUsageSummary.textContent = `${activeStays.length} khách - ${services.length} dịch vụ`;
        }

        renderServiceStayList(activeStays);
        renderServiceSelects(activeStays, services, preferredBookingCode);
        renderServiceSelection();
    }

    function renderServiceStayList(activeStays) {
        if (!elements.serviceStayList) return;

        if (activeStays.length === 0) {
            elements.serviceStayList.innerHTML = `<div class="arrival-empty">Hiện chưa có khách đang lưu trú.</div>`;
            return;
        }

        elements.serviceStayList.innerHTML = activeStays.map(function (stay) {
            const rooms = (stay.roomNumbers || []).length > 0
                ? `Phòng ${stay.roomNumbers.map(escapeHtml).join(", ")}`
                : "Chưa gán phòng";
            const serviceLines = (stay.serviceLines || []).slice(0, 3);
            const lineHtml = serviceLines.length === 0
                ? `<div class="service-line-empty">Chưa phát sinh dịch vụ.</div>`
                : serviceLines.map(function (line) {
                    return `
                        <div class="service-line">
                            <span>${escapeHtml(line.serviceName)} x${line.quantity}</span>
                            <strong>${formatMoney(line.total)}</strong>
                        </div>
                    `;
                }).join("");

            return `
                <article class="service-stay-card" data-service-stay="${escapeHtml(stay.bookingCode)}">
                    <div class="service-stay-main">
                        <span class="arrival-code">${escapeHtml(stay.bookingCode)}</span>
                        <h3>${escapeHtml(stay.customerName)}</h3>
                        <p>${escapeHtml(rooms)} · Trả ${escapeHtml(stay.checkOutDate)}</p>
                    </div>
                    <div class="service-stay-total">
                        <span>Dịch vụ</span>
                        <strong>${formatMoney(stay.serviceTotal)}</strong>
                    </div>
                    <div class="service-lines">${lineHtml}</div>
                </article>
            `;
        }).join("");

        elements.serviceStayList.querySelectorAll("[data-service-stay]").forEach(function (card) {
            card.addEventListener("click", function () {
                if (elements.serviceBookingSelect) {
                    elements.serviceBookingSelect.value = card.dataset.serviceStay;
                }
                renderServiceSelection();
            });
        });
    }

    function renderServiceSelects(activeStays, services, preferredBookingCode) {
        if (elements.serviceBookingSelect) {
            elements.serviceBookingSelect.innerHTML = activeStays.map(function (stay) {
                const rooms = (stay.roomNumbers || []).length > 0
                    ? ` - phòng ${stay.roomNumbers.join(", ")}`
                    : "";
                return `<option value="${escapeHtml(stay.bookingCode)}">${escapeHtml(stay.bookingCode)} - ${escapeHtml(stay.customerName)}${escapeHtml(rooms)}</option>`;
            }).join("");

            const fallbackCode = activeStays[0]?.bookingCode || "";
            elements.serviceBookingSelect.value = activeStays.some(function (stay) {
                return stay.bookingCode === preferredBookingCode;
            }) ? preferredBookingCode : fallbackCode;
            elements.serviceBookingSelect.disabled = activeStays.length === 0;
        }

        if (elements.serviceOptionSelect) {
            elements.serviceOptionSelect.innerHTML = services.map(function (service) {
                return `<option value="${escapeHtml(service.serviceId)}" data-price="${service.unitPrice}">${escapeHtml(service.serviceName)} - ${formatMoney(service.unitPrice)}/${escapeHtml(service.unit)}</option>`;
            }).join("");
            elements.serviceOptionSelect.disabled = services.length === 0;
        }

        if (elements.serviceQuantityInput) {
            elements.serviceQuantityInput.disabled = activeStays.length === 0 || services.length === 0;
        }

        if (elements.serviceNoteInput) {
            elements.serviceNoteInput.disabled = activeStays.length === 0 || services.length === 0;
        }

        if (elements.addServiceUsageBtn) {
            elements.addServiceUsageBtn.disabled = activeStays.length === 0 || services.length === 0;
        }
    }

    function renderServiceSelection() {
        const selectedCode = elements.serviceBookingSelect?.value || "";
        elements.serviceStayList?.querySelectorAll("[data-service-stay]").forEach(function (card) {
            card.classList.toggle("selected", card.dataset.serviceStay === selectedCode);
        });
        updateServiceEstimate();
    }

    function updateServiceEstimate() {
        if (!elements.serviceLineEstimate) return;

        const option = elements.serviceOptionSelect?.selectedOptions?.[0];
        const price = Number(option?.dataset.price || 0);
        const quantity = Math.max(Number(elements.serviceQuantityInput?.value || 0), 0);
        elements.serviceLineEstimate.textContent = formatMoney(price * quantity);
    }

    async function addServiceUsage(event) {
        event.preventDefault();

        const bookingCode = elements.serviceBookingSelect?.value || "";
        const serviceId = elements.serviceOptionSelect?.value || "";
        const quantity = Number(elements.serviceQuantityInput?.value || 0);
        const note = elements.serviceNoteInput?.value || "";

        if (!bookingCode || !serviceId || quantity <= 0) {
            showAlert("Chọn booking, dịch vụ và nhập số lượng hợp lệ trước khi lưu.", "warning");
            return;
        }

        setButtonBusy(elements.addServiceUsageBtn, true, "Đang lưu...");
        try {
            const result = await fetchJson("/Receptionist/ServiceUsage", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    bookingCode,
                    serviceId,
                    quantity,
                    note
                })
            });

            if (!result.success) {
                showAlert(result.message || "Không ghi nhận được dịch vụ.", "danger");
                return;
            }

            if (elements.serviceQuantityInput) {
                elements.serviceQuantityInput.value = "1";
            }
            if (elements.serviceNoteInput) {
                elements.serviceNoteInput.value = "";
            }

            await loadServiceUsage(result.bookingCode || bookingCode);
            showAlert(`${result.message} Tổng dịch vụ: ${formatMoney(result.serviceTotal)}.`, "success");
        } catch (error) {
            showAlert(error.message, "danger");
        } finally {
            setButtonBusy(elements.addServiceUsageBtn, false);
        }
    }

    async function loadCheckoutList(preferredBookingCode) {
        const preferredCode = typeof preferredBookingCode === "string"
            ? preferredBookingCode
            : state.selectedCheckoutCode;

        setStep("checkout");
        if (elements.checkoutSummary) {
            elements.checkoutSummary.textContent = "Đang tải...";
        }
        if (elements.checkoutStayList) {
            elements.checkoutStayList.innerHTML = `<div class="arrival-empty">Đang tải danh sách khách đang ở...</div>`;
        }

        try {
            const result = await fetchJson("/Receptionist/Checkout");
            if (!result.success) {
                showAlert(result.message || "Không tải được dữ liệu check-out.", "danger");
                return;
            }

            state.checkoutList = result;
            renderCheckoutList(preferredCode);
            showAlert(result.message, "info");
        } catch (error) {
            showAlert(error.message, "danger");
        }
    }

    function renderCheckoutList(preferredBookingCode) {
        const stays = state.checkoutList?.activeStays || [];
        if (elements.checkoutSummary) {
            elements.checkoutSummary.textContent = `${stays.length} khách đang ở`;
        }

        if (!elements.checkoutStayList) return;
        if (stays.length === 0) {
            elements.checkoutStayList.innerHTML = `<div class="arrival-empty">Hiện chưa có khách đang lưu trú để check-out.</div>`;
            state.selectedCheckoutCode = "";
            renderCheckoutDetail(null);
            return;
        }

        const selectedCode = stays.some(function (stay) {
            return stay.bookingCode === preferredBookingCode;
        }) ? preferredBookingCode : stays[0].bookingCode;
        state.selectedCheckoutCode = selectedCode || "";

        elements.checkoutStayList.innerHTML = stays.map(function (stay) {
            const rooms = (stay.roomNumbers || []).length > 0
                ? `Phòng ${stay.roomNumbers.map(escapeHtml).join(", ")}`
                : "Chưa gán phòng";
            const remainingClass = stay.remainingAmount > 0 ? "due" : "paid";

            return `
                <article class="checkout-stay-card ${remainingClass} ${stay.bookingCode === selectedCode ? "selected" : ""}" data-checkout-stay="${escapeHtml(stay.bookingCode)}">
                    <div class="service-stay-main">
                        <span class="arrival-code">${escapeHtml(stay.bookingCode)}</span>
                        <h3>${escapeHtml(stay.customerName)}</h3>
                        <p>${escapeHtml(rooms)} · ${escapeHtml(stay.checkInDate)} - ${escapeHtml(stay.checkOutDate)}</p>
                    </div>
                    <div class="checkout-stay-money">
                        <span>${stay.remainingAmount > 0 ? "Còn thu" : "Đã thu đủ"}</span>
                        <strong>${formatMoney(stay.remainingAmount)}</strong>
                    </div>
                </article>
            `;
        }).join("");

        elements.checkoutStayList.querySelectorAll("[data-checkout-stay]").forEach(function (card) {
            card.addEventListener("click", function () {
                state.selectedCheckoutCode = card.dataset.checkoutStay || "";
                renderCheckoutList(state.selectedCheckoutCode);
            });
        });

        renderCheckoutDetail(getSelectedCheckoutStay());
    }

    function getSelectedCheckoutStay() {
        const stays = state.checkoutList?.activeStays || [];
        return stays.find(function (stay) {
            return stay.bookingCode === state.selectedCheckoutCode;
        }) || null;
    }

    function renderCheckoutDetail(stay) {
        if (!elements.checkoutDetail) return;

        if (!stay) {
            elements.checkoutDetail.innerHTML = "Chọn một khách để xem hóa đơn.";
            elements.checkoutDetail.classList.add("checkout-detail-empty");
            if (elements.checkoutBookingCode) elements.checkoutBookingCode.value = "";
            if (elements.checkoutPaymentAmount) elements.checkoutPaymentAmount.value = "0";
            if (elements.confirmCheckoutBtn) elements.confirmCheckoutBtn.disabled = true;
            return;
        }

        elements.checkoutDetail.classList.remove("checkout-detail-empty");
        const roomLines = stay.roomLines || [];
        const serviceLines = stay.serviceLines || [];
        const roomsHtml = roomLines.length === 0
            ? `<div class="checkout-line muted">Chưa có dòng phòng.</div>`
            : roomLines.map(function (line) {
                const room = line.roomNumber ? ` · Phòng ${escapeHtml(line.roomNumber)}` : "";
                return `
                    <div class="checkout-line">
                        <span>${escapeHtml(line.roomTypeName)}${room}</span>
                        <strong>${formatMoney(line.total)}</strong>
                    </div>
                `;
            }).join("");
        const servicesHtml = serviceLines.length === 0
            ? `<div class="checkout-line muted">Chưa phát sinh dịch vụ.</div>`
            : serviceLines.map(function (line) {
                return `
                    <div class="checkout-line">
                        <span>${escapeHtml(line.serviceName)} x${line.quantity}</span>
                        <strong>${formatMoney(line.total)}</strong>
                    </div>
                `;
            }).join("");

        elements.checkoutDetail.innerHTML = `
            <div class="checkout-guest-box">
                <div>
                    <span class="arrival-code">${escapeHtml(stay.bookingCode)}</span>
                    <h4>${escapeHtml(stay.customerName)}</h4>
                    <p>${escapeHtml(stay.phoneNumber || "Chưa có số điện thoại")}</p>
                </div>
                <div>
                    <span>${stay.nights} đêm</span>
                    <strong>${escapeHtml(stay.checkInDate)} - ${escapeHtml(stay.checkOutDate)}</strong>
                </div>
            </div>
            <div class="checkout-lines">
                <h4>Tiền phòng</h4>
                ${roomsHtml}
                <h4>Dịch vụ</h4>
                ${servicesHtml}
            </div>
            <div class="checkout-total-box">
                ${moneyRow("Tiền phòng", stay.roomTotal)}
                ${moneyRow("Dịch vụ", stay.serviceTotal)}
                ${stay.discountAmount > 0 ? moneyRow("Giảm giá", -stay.discountAmount) : ""}
                ${moneyRow("Tổng hóa đơn", stay.grandTotal, true)}
                ${moneyRow("Đã thanh toán", stay.paidAmount)}
                ${moneyRow("Còn phải thu", stay.remainingAmount, true)}
            </div>
        `;

        if (elements.checkoutBookingCode) {
            elements.checkoutBookingCode.value = stay.bookingCode;
        }
        if (elements.checkoutPaymentAmount) {
            elements.checkoutPaymentAmount.value = String(Math.max(stay.remainingAmount || 0, 0));
        }
        if (elements.checkoutNote) {
            elements.checkoutNote.value = "";
        }
        if (elements.confirmCheckoutBtn) {
            elements.confirmCheckoutBtn.disabled = false;
        }
    }

    async function submitCheckout(event) {
        event.preventDefault();

        const bookingCode = elements.checkoutBookingCode?.value || state.selectedCheckoutCode;
        const paymentAmount = Number(elements.checkoutPaymentAmount?.value || 0);
        const paymentMethod = elements.checkoutPaymentMethod?.value || "";
        const note = elements.checkoutNote?.value || "";

        if (!bookingCode) {
            showAlert("Chọn khách cần check-out trước.", "warning");
            return;
        }

        if (paymentAmount < 0) {
            showAlert("Số tiền thu thêm không hợp lệ.", "warning");
            return;
        }

        setButtonBusy(elements.confirmCheckoutBtn, true, "Đang check-out...");
        try {
            const result = await fetchJson("/Receptionist/Checkout", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    bookingCode,
                    paymentAmount,
                    paymentMethod,
                    note
                })
            });

            if (!result.success) {
                showAlert(result.message || "Check-out thất bại.", "danger");
                return;
            }

            state.selectedCheckoutCode = "";
            state.roomMap = null;
            state.serviceUsage = null;
            await loadCheckoutList();
            showAlert(`${result.message} Đã trả phòng ${result.releasedRooms?.join(", ") || ""}.`, "success");
        } catch (error) {
            showAlert(error.message, "danger");
        } finally {
            setButtonBusy(elements.confirmCheckoutBtn, false);
        }
    }

    function moneyRow(label, value, strong) {
        return `
            <div class="checkout-money-row ${strong ? "strong" : ""}">
                <span>${escapeHtml(label)}</span>
                <strong>${formatMoney(value)}</strong>
            </div>
        `;
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
            const result = await fetchJson(`/Receptionist/Rooms?bookingCode=${encodeURIComponent(state.booking.bookingCode)}`);
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
            const result = await fetchJson("/Receptionist/CheckIn", {
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
        state.lastWorkflowStep = "scan";
        elements.bookingCodeInput.value = "";
        elements.guestDetails.innerHTML = "";
        elements.bookingDetails.innerHTML = "";
        elements.roomRequirements.innerHTML = "";
        elements.roomGroups.innerHTML = "";
        elements.confirmRooms.innerHTML = "";
        elements.assignedRooms.innerHTML = "";
        elements.alertArea.innerHTML = "";
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

    function formatDateLabel(value) {
        if (!value) return "";

        const parts = String(value).split("-");
        if (parts.length !== 3) {
            return value;
        }

        return `${parts[2]}/${parts[1]}/${parts[0]}`;
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
