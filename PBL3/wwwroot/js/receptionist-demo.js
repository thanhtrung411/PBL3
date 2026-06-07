(function () {
    const state = {
        currentStep: "scan",
        lastWorkflowStep: "scan",
        booking: null,
        roomPlan: null,
        roomMap: null,
        roomMapFilter: "all",
        roomMapTypeFilter: "all",
        roomMapServiceUsage: null,
        selectedRoomMapRoomId: "",
        serviceUsage: null,
        checkoutList: null,
        checkoutSearch: "",
        checkoutScope: "today",
        checkoutSort: "checkoutDateAsc",
        walkInAvailability: null,
        walkInPromotionPreview: null,
        selectedWalkInRooms: new Map(),
        walkInStep: "info",
        selectedCheckoutCode: "",
        selectedRooms: new Map(),
        scanner: null,
        scannerRunning: false
    };

    let tomBookingSelect = null;
    let tomOptionSelect = null;

    const elements = {
        alertArea: document.getElementById("alertArea"),
        workflowStrip: document.querySelector(".workflow-strip"),
        receptionHeaderTitle: document.getElementById("receptionHeaderTitle"),
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
        roomMapTypeFilter: document.getElementById("roomMapTypeFilter"),
        roomMapDetailModal: document.getElementById("roomMapDetailModal"),
        roomMapDetailBody: document.getElementById("roomMapDetailBody"),
        roomMapDetailClose: document.getElementById("roomMapDetailClose"),
        roomMapFloors: document.getElementById("roomMapFloors"),
        serviceUsageRefreshBtn: document.getElementById("serviceUsageRefreshBtn"),
        serviceUsageForm: document.getElementById("serviceUsageForm"),
        serviceSelectedStayInfo: document.getElementById("serviceSelectedStayInfo"),
        serviceSelectedServiceInfo: document.getElementById("serviceSelectedServiceInfo"),
        serviceBookingSelect: document.getElementById("serviceBookingSelect"),
        serviceBookingOptions: document.getElementById("serviceBookingOptions"),
        serviceOptionSelect: document.getElementById("serviceOptionSelect"),
        serviceOptionOptions: document.getElementById("serviceOptionOptions"),
        serviceQuantityInput: document.getElementById("serviceQuantityInput"),
        serviceNoteInput: document.getElementById("serviceNoteInput"),
        serviceLineEstimate: document.getElementById("serviceLineEstimate"),
        addServiceUsageBtn: document.getElementById("addServiceUsageBtn"),
        checkoutRefreshBtn: document.getElementById("checkoutRefreshBtn"),
        checkoutSummary: document.getElementById("checkoutSummary"),
        checkoutSearchInput: document.getElementById("checkoutSearchInput"),
        checkoutScopeSelect: document.getElementById("checkoutScopeSelect"),
        checkoutSortSelect: document.getElementById("checkoutSortSelect"),
        checkoutListPanel: document.getElementById("checkoutListPanel"),
        checkoutStayList: document.getElementById("checkoutStayList"),
        checkoutDetailPanel: document.getElementById("checkoutDetailPanel"),
        checkoutPaymentResultPanel: document.getElementById("checkoutPaymentResultPanel"),
        checkoutPaymentResultIcon: document.getElementById("checkoutPaymentResultIcon"),
        checkoutPaymentResultTitle: document.getElementById("checkoutPaymentResultTitle"),
        checkoutPaymentResultMessage: document.getElementById("checkoutPaymentResultMessage"),
        checkoutPaymentResultBooking: document.getElementById("checkoutPaymentResultBooking"),
        checkoutPaymentResultAmount: document.getElementById("checkoutPaymentResultAmount"),
        checkoutPaymentResultTransaction: document.getElementById("checkoutPaymentResultTransaction"),
        checkoutPaymentResultBank: document.getElementById("checkoutPaymentResultBank"),
        checkoutResultBackBtn: document.getElementById("checkoutResultBackBtn"),
        checkoutResultRefreshBtn: document.getElementById("checkoutResultRefreshBtn"),
        checkoutBackToListBtn: document.getElementById("checkoutBackToListBtn"),
        checkoutDetail: document.getElementById("checkoutDetail"),
        checkoutMoneySummary: document.getElementById("checkoutMoneySummary"),
        checkoutPaymentForm: document.getElementById("checkoutPaymentForm"),
        checkoutBookingCode: document.getElementById("checkoutBookingCode"),
        checkoutPaymentAmount: document.getElementById("checkoutPaymentAmount"),
        checkoutPaymentMethod: document.getElementById("checkoutPaymentMethod"),
        checkoutNote: document.getElementById("checkoutNote"),
        checkoutSendEmail: document.getElementById("checkoutSendEmail"),
        checkoutPrintInvoice: document.getElementById("checkoutPrintInvoice"),
        checkoutReceiptEmail: document.getElementById("checkoutReceiptEmail"),
        confirmCheckoutBtn: document.getElementById("confirmCheckoutBtn"),
        walkInResetBtn: document.getElementById("walkInResetBtn"),
        walkInForm: document.getElementById("walkInForm"),
        walkInCustomerName: document.getElementById("walkInCustomerName"),
        walkInPhone: document.getElementById("walkInPhone"),
        walkInIdentity: document.getElementById("walkInIdentity"),
        walkInEmail: document.getElementById("walkInEmail"),
        walkInGuestCount: document.getElementById("walkInGuestCount"),
        walkInCheckinDate: document.getElementById("walkInCheckinDate"),
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
        walkInViews: document.querySelectorAll("[data-walkin-view]"),
        walkInStepIndicators: document.querySelectorAll("[data-walkin-step-indicator]"),
        walkInSelectedCount: document.getElementById("walkInSelectedCount"),
        walkInSelectedCapacity: document.getElementById("walkInSelectedCapacity"),
        walkInSelectedTypes: document.getElementById("walkInSelectedTypes"),
        walkInStayNights: document.getElementById("walkInStayNights"),
        walkInGoPaymentBtn: document.getElementById("walkInGoPaymentBtn"),
        walkInPaymentChoices: document.querySelectorAll('input[name="walkInPaymentChoice"]'),
        walkInCashBox: document.getElementById("walkInCashBox"),
        walkInPaymentDue: document.getElementById("walkInPaymentDue"),
        walkInChangeAmount: document.getElementById("walkInChangeAmount"),
        walkInReviewBtn: document.getElementById("walkInReviewBtn"),
        walkInConfirmSummary: document.getElementById("walkInConfirmSummary"),
        walkInDoneMessage: document.getElementById("walkInDoneMessage"),
        walkInDoneRooms: document.getElementById("walkInDoneRooms"),
        walkInNewBtn: document.getElementById("walkInNewBtn"),
        walkInBackBtns: document.querySelectorAll("[data-walkin-back]")
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
    elements.refreshCheckInBtn?.addEventListener("click", function () {
        if (state.currentStep === "walkIn") {
            resetWalkIn();
            return;
        }

        resetFlow();
    });
    elements.walkInNavBtn?.addEventListener("click", showWalkIn);
    elements.walkInForm?.addEventListener("submit", function (event) {
        event.preventDefault();
    });
    elements.walkInForm?.addEventListener("keydown", function (event) {
        if (event.key !== "Enter" || event.target?.tagName === "TEXTAREA") return;
        event.preventDefault();
    });
    elements.walkInLoadRoomsBtn?.addEventListener("click", loadWalkInAvailability);
    elements.walkInResetBtn?.addEventListener("click", resetWalkIn);
    elements.walkInEditGuestBtn?.addEventListener("click", function () {
        showWalkInView("info");
    });
    elements.walkInGoPaymentBtn?.addEventListener("click", showWalkInPaymentStep);
    elements.walkInReviewBtn?.addEventListener("click", showWalkInConfirmStep);
    elements.walkInConfirmBtn?.addEventListener("click", submitWalkInCheckIn);
    bindCurrencyInput(elements.walkInPaymentAmount, updateWalkInSelectionState);
    elements.walkInPaymentChoices?.forEach(function (item) {
        item.addEventListener("change", updateWalkInPaymentUi);
    });
    elements.walkInBackBtns?.forEach(function (button) {
        button.addEventListener("click", function () {
            showWalkInView(button.dataset.walkinBack);
        });
    });
    elements.walkInNewBtn?.addEventListener("click", resetWalkIn);
    elements.walkInCheckinDate?.addEventListener("change", function () {
        ensureWalkInDateOrder();
        state.walkInAvailability = null;
        state.walkInPromotionPreview = null;
        state.selectedWalkInRooms.clear();
        renderWalkInAvailability();
    });
    elements.walkInCheckoutDate?.addEventListener("change", function () {
        state.walkInAvailability = null;
        state.walkInPromotionPreview = null;
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
    elements.serviceBookingSelect?.addEventListener("input", renderServiceSelection);
    elements.serviceBookingSelect?.addEventListener("change", renderServiceSelection);
    elements.serviceOptionSelect?.addEventListener("input", updateServiceEstimate);
    elements.serviceOptionSelect?.addEventListener("change", updateServiceEstimate);
    elements.serviceQuantityInput?.addEventListener("input", updateServiceEstimate);
    elements.checkoutNavBtn?.addEventListener("click", loadCheckoutList);
    elements.checkoutRefreshBtn?.addEventListener("click", loadCheckoutList);
    elements.checkoutBackToListBtn?.addEventListener("click", showCheckoutListPage);
    elements.checkoutResultBackBtn?.addEventListener("click", loadCheckoutList);
    elements.checkoutResultRefreshBtn?.addEventListener("click", loadCheckoutList);
    elements.checkoutSearchInput?.addEventListener("input", function () {
        state.checkoutSearch = elements.checkoutSearchInput.value || "";
        state.selectedCheckoutCode = "";
        renderCheckoutList();
    });
    elements.checkoutScopeSelect?.addEventListener("change", function () {
        state.checkoutScope = elements.checkoutScopeSelect.value || "today";
        state.selectedCheckoutCode = "";
        renderCheckoutList();
    });
    elements.checkoutSortSelect?.addEventListener("change", function () {
        state.checkoutSort = elements.checkoutSortSelect.value || "checkoutDateAsc";
        renderCheckoutList(state.selectedCheckoutCode);
    });
    elements.checkoutPaymentForm?.addEventListener("submit", submitCheckout);
    bindCurrencyInput(elements.checkoutPaymentAmount);
    elements.checkoutPaymentMethod?.addEventListener("change", updateCheckoutPaymentUi);
    elements.checkoutSendEmail?.addEventListener("change", syncCheckoutEmailField);
    elements.checkoutReceiptEmail?.addEventListener("input", function () {
        if (elements.checkoutReceiptEmail.value.trim() && elements.checkoutSendEmail) {
            elements.checkoutSendEmail.checked = true;
            syncCheckoutEmailField();
        }
    });
    document.querySelectorAll("[data-room-map-filter]").forEach(function (button) {
        button.addEventListener("click", function () {
            state.roomMapFilter = button.dataset.roomMapFilter || "all";
            document.querySelectorAll("[data-room-map-filter]").forEach(function (item) {
                item.classList.toggle("active", item === button);
            });
            renderRoomMap();
        });
    });
    elements.roomMapTypeFilter?.addEventListener("change", function () {
        state.roomMapTypeFilter = elements.roomMapTypeFilter.value || "all";
        renderRoomMap();
    });
    elements.roomMapFloors?.addEventListener("click", function (event) {
        const tile = event.target.closest("[data-room-map-room-id]");
        if (!tile) return;

        selectRoomMapRoom(tile.dataset.roomMapRoomId);
    });
    elements.roomMapFloors?.addEventListener("keydown", function (event) {
        if (event.key !== "Enter" && event.key !== " ") return;

        const tile = event.target.closest("[data-room-map-room-id]");
        if (!tile) return;

        event.preventDefault();
        selectRoomMapRoom(tile.dataset.roomMapRoomId);
    });
    elements.roomMapDetailClose?.addEventListener("click", closeRoomMapDetailModal);
    elements.roomMapDetailBody?.addEventListener("click", function (event) {
        const button = event.target.closest("[data-room-map-maintenance]");
        if (!button) return;
        updateRoomMaintenance(button);
    });
    elements.roomMapDetailModal?.addEventListener("click", function (event) {
        if (event.target === elements.roomMapDetailModal) {
            closeRoomMapDetailModal();
        }
    });
    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && elements.roomMapDetailModal && !elements.roomMapDetailModal.hidden) {
            closeRoomMapDetailModal();
        }
    });

    document.querySelectorAll("[data-step-indicator]").forEach(function (button) {
        button.addEventListener("click", function () {
            goToStepFromIndicator(button.dataset.stepIndicator);
        });
    });

    elements.walkInStepIndicators?.forEach(function (button) {
        button.addEventListener("click", function () {
            goToWalkInStepFromIndicator(button.dataset.walkinStepIndicator);
        });
    });

    document.querySelectorAll("[data-back-step]").forEach(function (button) {
        button.addEventListener("click", function () {
            setStep(button.dataset.backStep);
        });
    });

    handleReceptionPaymentReturn();
    handleInitialOpenTarget();
    startRealtimeBadges();

    function startRealtimeBadges() {
        async function fetchCounts() {
            try {
                const todayRes = await fetchJson("/Receptionist/Today");
                if (todayRes && todayRes.success) {
                    const todayCount = (todayRes.bookings || []).length;
                    const badge = document.getElementById("todayArrivalsBadge");
                    if (badge) {
                        badge.textContent = todayCount;
                        badge.style.display = todayCount > 0 ? "block" : "none";
                    }
                }
                
                const checkoutRes = await fetchJson("/Receptionist/Checkout");
                if (checkoutRes && checkoutRes.success) {
                    const checkoutCount = (checkoutRes.activeStays || []).length;
                    const badge = document.getElementById("checkoutBadge");
                    if (badge) {
                        badge.textContent = checkoutCount;
                        badge.style.display = checkoutCount > 0 ? "block" : "none";
                    }
                }
            } catch(e) {
                console.error("Failed to fetch realtime badges", e);
            }
        }
        
        fetchCounts();
        setInterval(fetchCounts, 15000);
    }

    function handleInitialOpenTarget() {
        const query = new URLSearchParams(window.location.search);
        if (query.get("open") === "checkout") {
            loadCheckoutList();
            query.delete("open");
            const nextUrl = `${window.location.pathname}${query.toString() ? `?${query}` : ""}${window.location.hash}`;
            window.history.replaceState({}, document.title, nextUrl);
        }
    }

    function setStep(step) {
        if (step !== "scan" && state.scannerRunning) {
            stopScanner().catch(function () { });
        }

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

        updateReceptionShell(step);
        updateWorkflowIndicators();
        elements.checkInNavBtn?.classList.toggle("active", step !== "today" && step !== "roomMap" && step !== "services" && step !== "checkout" && step !== "walkIn");
        elements.walkInNavBtn?.classList.toggle("active", step === "walkIn");
        elements.todayArrivalsNavBtn?.classList.toggle("active", step === "today");
        elements.roomMapNavBtn?.classList.toggle("active", step === "roomMap");
        elements.serviceUsageNavBtn?.classList.toggle("active", step === "services");
        elements.checkoutNavBtn?.classList.toggle("active", step === "checkout");

        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    async function handleReceptionPaymentReturn() {
        const query = new URLSearchParams(window.location.search);
        const walkInStatus = query.get("walkInPaymentStatus");
        const checkoutStatus = query.get("checkoutPaymentStatus");
        if (!walkInStatus && !checkoutStatus) {
            return;
        }

        const bookingCode = query.get("bookingCode") || "";
        const message = query.get("message") || "";
        if (checkoutStatus) {
            setStep("checkout");
            if (checkoutStatus === "success") {
                renderCheckoutPaymentResult({
                    status: checkoutStatus,
                    bookingCode,
                    amount: Number(query.get("amount") || 0),
                    transactionNo: query.get("transactionNo") || "",
                    bankCode: query.get("bankCode") || "",
                    message
                });
                showAlert(message || "Thanh toán VNPay check-out thành công.", "success");
                
                if (query.get("printInvoice") === "true") {
                    const savedStayStr = sessionStorage.getItem("vnpay_checkout_stay_" + bookingCode);
                    if (savedStayStr) {
                        try {
                            const stay = JSON.parse(savedStayStr);
                            const paidAmount = Number(query.get("amount") || 0) + (stay.paidAmount || 0);
                            printCheckoutInvoice(stay, { 
                                receiptEmail: query.get("receiptEmail"), 
                                paymentMethod: "VNPay", 
                                result: { paidAmount: paidAmount, remainingAmount: 0 } 
                            });
                        } catch (e) {
                            console.error("Failed to parse saved stay", e);
                        }
                    }
                }
                sessionStorage.removeItem("vnpay_checkout_stay_" + bookingCode);
            } else {
                state.selectedCheckoutCode = bookingCode;
                await loadCheckoutList();
                showAlert(message || "Thanh toán VNPay đã bị hủy hoặc không thành công. Vui lòng chọn lại phương thức thanh toán.", checkoutStatus === "cancelled" ? "warning" : "danger");
            }
            query.delete("checkoutPaymentStatus");
            query.delete("bookingCode");
            query.delete("amount");
            query.delete("transactionNo");
            query.delete("bankCode");
            query.delete("message");
            const nextUrl = `${window.location.pathname}${query.toString() ? `?${query}` : ""}${window.location.hash}`;
            window.history.replaceState({}, document.title, nextUrl);
            return;
        }

        const status = walkInStatus;
        showWalkIn();

        if (status === "success") {
            renderWalkInDone({
                bookingCode,
                message: bookingCode
                    ? `Thanh toán VNPay thành công. Mã đặt phòng ${bookingCode}.`
                    : "Thanh toán VNPay thành công."
            });
            showWalkInView("done");
            if (bookingCode) {
                await loadWalkInDoneBooking(bookingCode);
            }
            showAlert(message || "Thanh toán VNPay thành công.", "success");
        } else {
            resetWalkIn();
            showAlert(
                message || "Thanh toán VNPay chưa hoàn tất. Các phòng đã chọn đã được hủy giữ.",
                status === "cancelled" ? "warning" : "danger");
        }

        query.delete("walkInPaymentStatus");
        query.delete("bookingCode");
        query.delete("message");
        const nextUrl = `${window.location.pathname}${query.toString() ? `?${query}` : ""}${window.location.hash}`;
        window.history.replaceState({}, document.title, nextUrl);
    }

    async function loadWalkInDoneBooking(bookingCode) {
        try {
            const result = await fetchJson(`/Receptionist/Lookup?code=${encodeURIComponent(bookingCode)}`);
            if (result.success && result.booking) {
                renderWalkInDone({
                    bookingCode,
                    message: `Thanh toán VNPay thành công. Mã đặt phòng ${bookingCode}.`,
                    booking: result.booking,
                    rooms: result.booking.assignedRooms || []
                });
            }
        } catch {
            renderWalkInDone({
                bookingCode,
                message: `Thanh toán VNPay thành công. Mã đặt phòng ${bookingCode}.`,
                rooms: []
            });
        }
    }

    function updateReceptionShell(step) {
        const isWalkIn = step === "walkIn";
        const isCheckIn = step === "scan" || step === "details" || step === "rooms" || step === "confirm" || step === "done";
        if (elements.receptionHeaderTitle) {
            elements.receptionHeaderTitle.textContent = isWalkIn ? "Check-in khách vãng lai" : getHeaderTitle(step);
        }

        const labels = isWalkIn
            ? ["Thông tin", "Chọn phòng", "Thanh toán", "Xác nhận", "Hoàn tất"]
            : ["Quét mã", "Thông tin", "Chọn phòng", "Xác nhận", "Hoàn tất"];
        document.querySelectorAll("[data-step-indicator] strong").forEach(function (label, index) {
            label.textContent = labels[index] || label.textContent;
        });
    }

    function getHeaderTitle(step) {
        if (step === "today") return "Khách đến hôm nay";
        if (step === "roomMap") return "Sơ đồ phòng";
        if (step === "services") return "Sử dụng dịch vụ";
        if (step === "checkout") return "Check-out khách lưu trú";
        return "Check-in khách đặt phòng";
    }

    function updateWorkflowIndicators() {
        const workflowSteps = ["scan", "details", "rooms", "confirm", "done"];
        const walkInSteps = ["info", "rooms", "payment", "confirm", "done"];
        const indicators = Array.from(document.querySelectorAll("[data-step-indicator]"));

        if (state.currentStep === "walkIn") {
            const activeIndex = Math.max(walkInSteps.indexOf(state.walkInStep), 0);
            indicators.forEach(function (indicator, index) {
                const walkInStep = walkInSteps[index];
                indicator.classList.toggle("active", index <= activeIndex);
                indicator.classList.toggle("completed", index < activeIndex);
                indicator.classList.toggle("is-available", canNavigateToWalkInStep(walkInStep));
            });
            return;
        }

        const activeIndex = workflowSteps.indexOf(state.currentStep);
        indicators.forEach(function (indicator) {
            const index = workflowSteps.indexOf(indicator.dataset.stepIndicator);
            indicator.classList.toggle("active", index <= activeIndex);
            indicator.classList.toggle("completed", index < activeIndex);
            indicator.classList.toggle("is-available", canNavigateToStep(indicator.dataset.stepIndicator));
        });
    }

    function goToStepFromIndicator(step) {
        if (state.currentStep === "walkIn") {
            const map = {
                scan: "info",
                details: "rooms",
                rooms: "payment",
                confirm: "confirm",
                done: "done"
            };
            const walkInStep = map[step] || "info";
            goToWalkInStepFromIndicator(walkInStep);
            return;
        }

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
        const today = new Date();
        const tomorrow = new Date(today);
        tomorrow.setDate(tomorrow.getDate() + 1);
        if (elements.walkInCheckinDate && !elements.walkInCheckinDate.value) {
            elements.walkInCheckinDate.value = formatInputDate(today);
        }
        if (elements.walkInCheckoutDate && !elements.walkInCheckoutDate.value) {
            elements.walkInCheckoutDate.value = formatInputDate(tomorrow);
        }
        showWalkInView(state.walkInAvailability ? state.walkInStep : "info");
        renderWalkInGuestSummary();
        renderWalkInAvailability();
    }

    function resetWalkIn() {
        state.walkInAvailability = null;
        state.walkInPromotionPreview = null;
        state.selectedWalkInRooms.clear();
        elements.walkInForm?.reset();
        if (elements.walkInGuestCount) elements.walkInGuestCount.value = "1";
        if (elements.walkInNationality) elements.walkInNationality.value = "Việt Nam";
        setCurrencyInputValue(elements.walkInPaymentAmount, 0);
        state.walkInStep = "info";
        showWalkInView("info");
        showWalkIn();
    }

    async function loadWalkInAvailability() {
        if (!validateWalkInGuestInfo()) {
            return;
        }

        const checkinDate = elements.walkInCheckinDate?.value || "";
        const checkoutDate = elements.walkInCheckoutDate?.value || "";
        if (!checkinDate || !checkoutDate) {
            showAlert("Chọn ngày nhận và ngày trả phòng trước khi tìm phòng.", "warning");
            return;
        }

        setButtonBusy(elements.walkInLoadRoomsBtn, true, "Đang tải...");
        try {
            const result = await fetchJson(`/Receptionist/WalkInAvailability?checkInDate=${encodeURIComponent(checkinDate)}&checkOutDate=${encodeURIComponent(checkoutDate)}`);
            if (!result.success) {
                showAlert(result.message || "Không tải được phòng trống.", "danger");
                return;
            }

            state.walkInAvailability = result;
            state.selectedWalkInRooms.clear();
            renderWalkInGuestSummary();
            renderWalkInAvailability();
            showWalkInView("rooms");
        } catch (error) {
            showAlert(error.message, "danger");
        } finally {
            setButtonBusy(elements.walkInLoadRoomsBtn, false);
        }
    }

    function showWalkInView(view) {
        state.walkInStep = view || "info";
        elements.walkInViews?.forEach(function (item) {
            item.hidden = item.dataset.walkinView !== state.walkInStep;
        });
        updateWorkflowIndicators();
        updateWalkInPaymentUi();
    }

    function goToWalkInStepFromIndicator(step) {
        if (!canNavigateToWalkInStep(step)) {
            showAlert("Bước này chưa có đủ dữ liệu để mở.", "warning");
            return;
        }

        if (step === "confirm") {
            showWalkInConfirmStep();
            return;
        }
        showWalkInView(step);
    }

    function canNavigateToWalkInStep(step) {
        if (step === "info") return true;
        if (step === "rooms") return Boolean(state.walkInAvailability);
        if (step === "payment") return state.selectedWalkInRooms.size > 0;
        if (step === "confirm") {
            if (state.selectedWalkInRooms.size === 0) return false;
            return getWalkInPaymentMethod() === "vnpay" ||
                getCurrencyInputValue(elements.walkInPaymentAmount) + 0.01 >= getWalkInPayableTotal();
        }

        return step === "done" && state.walkInStep === "done";
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

        const checkinDate = elements.walkInCheckinDate?.value || "";
        const checkoutDate = elements.walkInCheckoutDate?.value || "";
        if (!checkinDate || !checkoutDate) {
            showAlert("Chọn ngày nhận và ngày trả phòng.", "warning");
            return false;
        }

        if (checkoutDate <= checkinDate) {
            showAlert("Ngày trả phòng phải sau ngày nhận phòng.", "warning");
            elements.walkInCheckoutDate?.focus();
            return false;
        }

        return true;
    }

    function renderWalkInGuestSummary() {
        if (!elements.walkInGuestSummary) return;

        const customerName = formatPersonName(elements.walkInCustomerName?.value.trim() || "Khách vãng lai");
        const phone = elements.walkInPhone?.value.trim() || "Chưa có SĐT";
        const identity = elements.walkInIdentity?.value.trim() || "Chưa có giấy tờ";
        const guestCount = Number(elements.walkInGuestCount?.value || 1);
        const checkinDate = elements.walkInCheckinDate?.value || "";
        const checkoutDate = elements.walkInCheckoutDate?.value || "";
        const checkinLabel = checkinDate ? formatDateLabel(checkinDate) : "Chưa chọn ngày nhận";
        const checkoutLabel = checkoutDate ? formatDateLabel(checkoutDate) : "Chưa chọn ngày trả";

        elements.walkInGuestSummary.innerHTML = `
            <div>
                <h3>${escapeHtml(customerName)}</h3>
            </div>
            <dl>
                <div><dt>SĐT</dt><dd>${escapeHtml(phone)}</dd></div>
                <div><dt>CCCD/Hộ chiếu</dt><dd>${escapeHtml(identity)}</dd></div>
                <div><dt>Số khách</dt><dd>${guestCount}</dd></div>
                <div><dt>Nhận phòng</dt><dd>${escapeHtml(checkinLabel)}</dd></div>
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
            const roomsByFloor = groupRoomsByFloor(group.rooms || []);
            const floorSections = roomsByFloor.map(function (floor) {
                const rooms = floor.rooms.map(function (room) {
                    const status = room.status || "available";
                    const disabled = room.isSelectable ? "" : "disabled";
                    return `
                        <button type="button"
                                class="room-tile compact ${escapeHtml(status)}"
                                data-walkin-room-id="${escapeHtml(room.roomId)}"
                                data-walkin-room-type="${escapeHtml(group.roomTypeId)}"
                                ${disabled}
                                aria-pressed="false">
                            <strong>${escapeHtml(formatDisplayText(room.roomNumber))}</strong>
                            <small>${escapeHtml(formatDisplayText(room.roomTypeName || group.roomTypeName))}</small>
                            <small>${escapeHtml(formatSentenceText(room.statusLabel || "Trống"))}</small>
                        </button>
                    `;
                }).join("");

                return `
                    <div class="walkin-floor-section">
                        <div class="walkin-floor-title">Tầng ${floor.floor}</div>
                        <div class="room-grid walkin-room-grid">${rooms}</div>
                    </div>
                `;
            }).join("");

            const totalRooms = group.rooms?.length || 0;
            const maxSelectable = Math.max(Number(group.maxSelectableRooms || 0), 0);
            const reservedCount = Math.max(Number(group.reservedForBookingCount || 0), 0);
            const reservedText = reservedCount > 0 ? ` · giữ trước ${reservedCount}` : "";
            return `
                <section class="room-type-section walkin-room-type" data-walkin-room-type-section="${escapeHtml(group.roomTypeId)}" data-max-selectable="${maxSelectable}">
                    <div class="room-type-header walkin-room-type-header">
                        <div>
                            <strong>${escapeHtml(formatDisplayText(group.roomTypeName))}</strong>
                            <span class="walkin-room-meta">${formatMoney(group.pricePerNight || 0)} / đêm · tối đa ${escapeHtml(String(group.capacity || 0))} khách/phòng · ${totalRooms} phòng${reservedText}</span>
                        </div>
                        <span class="room-counter" data-walkin-room-counter="${escapeHtml(group.roomTypeId)}">0/${maxSelectable} đã chọn</span>
                    </div>
                    ${floorSections}
                </section>
            `;
        }).join("");

        elements.walkInRoomGroups.querySelectorAll(".room-tile[data-walkin-room-id]").forEach(function (tile) {
            tile.addEventListener("click", function () {
                toggleWalkInRoom(tile.dataset.walkinRoomId);
            });
        });
        updateWalkInSelectionState();
    }

    function toggleWalkInRoom(roomId) {
        const room = findWalkInRoom(roomId);
        if (!room) return;
        if (!room.isSelectable) {
            showAlert(`Phòng ${room.roomNumber} hiện không thể chọn.`, "warning");
            return;
        }

        if (state.selectedWalkInRooms.has(roomId)) {
            state.selectedWalkInRooms.delete(roomId);
        } else {
            const group = findWalkInGroup(room.roomTypeId);
            const selectedForType = getSelectedWalkInRoomsByType(room.roomTypeId).length;
            const maxSelectable = Number(group?.maxSelectableRooms || 0);
            if (selectedForType >= maxSelectable) {
                showAlert(`${group?.roomTypeName || "Loại phòng này"} đã đạt giới hạn chọn vì cần giữ phòng cho đặt trước.`, "warning");
                markWalkInRoomTypeLocked(room.roomTypeId, true);
                return;
            }

            state.selectedWalkInRooms.set(roomId, room);
        }

        state.walkInPromotionPreview = null;
        updateWalkInSelectionState();
    }

    function findWalkInGroup(roomTypeId) {
        return (state.walkInAvailability?.groups || []).find(function (group) {
            return group.roomTypeId === roomTypeId;
        }) || null;
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

    function getSelectedWalkInRoomsByType(roomTypeId) {
        return Array.from(state.selectedWalkInRooms.values()).filter(function (room) {
            return room.roomTypeId === roomTypeId;
        });
    }

    function groupRoomsByFloor(rooms) {
        const floors = new Map();
        rooms.forEach(function (room) {
            const floor = room.floor || 0;
            if (!floors.has(floor)) {
                floors.set(floor, []);
            }
            floors.get(floor).push(room);
        });

        return Array.from(floors.entries())
            .sort(function (a, b) { return a[0] - b[0]; })
            .map(function ([floor, floorRooms]) {
                return {
                    floor,
                    rooms: floorRooms.sort(function (a, b) {
                        return String(a.roomNumber).localeCompare(String(b.roomNumber), "vi", { numeric: true });
                    })
                };
            });
    }

    function getWalkInTotal() {
        const nights = state.walkInAvailability?.nights || 0;
        return Array.from(state.selectedWalkInRooms.values())
            .reduce(function (total, room) {
                return total + ((room.pricePerNight || 0) * nights);
            }, 0);
    }

    function getWalkInDiscountAmount() {
        return Math.max(Number(state.walkInPromotionPreview?.discountAmount || 0), 0);
    }

    function getWalkInPayableTotal() {
        const total = getWalkInTotal();
        if (state.walkInPromotionPreview?.success) {
            return Math.max(Number(state.walkInPromotionPreview.grandTotal || total), 0);
        }

        return total;
    }

    async function refreshWalkInPromotionPreview() {
        state.walkInPromotionPreview = null;
        if (!state.walkInAvailability || state.selectedWalkInRooms.size === 0) {
            updateWalkInSelectionState();
            return null;
        }

        const result = await fetchJson("/Receptionist/WalkInPromotionPreview", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                checkInDate: elements.walkInCheckinDate?.value || "",
                checkOutDate: elements.walkInCheckoutDate?.value || "",
                roomIds: Array.from(state.selectedWalkInRooms.keys())
            })
        });

        state.walkInPromotionPreview = result?.success ? result : null;
        updateWalkInSelectionState();
        return state.walkInPromotionPreview;
    }

    function updateWalkInSelectionState() {
        const total = getWalkInTotal();
        const payableTotal = getWalkInPayableTotal();
        const selectedRooms = Array.from(state.selectedWalkInRooms.values());
        const selectedCapacity = selectedRooms.reduce(function (sum, room) {
            return sum + Number(room.capacity || 0);
        }, 0);
        const selectedTypes = Array.from(selectedRooms.reduce(function (types, room) {
            const roomTypeName = formatDisplayText(room.roomTypeName || "Phòng");
            types.set(roomTypeName, (types.get(roomTypeName) || 0) + 1);
            return types;
        }, new Map()).entries())
            .sort(function (a, b) {
                return a[0].localeCompare(b[0], "vi", { numeric: true });
            })
            .map(function ([roomTypeName, count]) {
                return `${formatDisplayText(roomTypeName)} x${count}`;
            });
        elements.walkInRoomGroups?.querySelectorAll("[data-walkin-room-id]").forEach(function (tile) {
            const selected = state.selectedWalkInRooms.has(tile.dataset.walkinRoomId);
            tile.classList.toggle("selected", selected);
            tile.setAttribute("aria-pressed", selected ? "true" : "false");
        });

        (state.walkInAvailability?.groups || []).forEach(function (group) {
            const selectedForType = getSelectedWalkInRoomsByType(group.roomTypeId).length;
            markWalkInRoomTypeLocked(group.roomTypeId, selectedForType >= Number(group.maxSelectableRooms || 0));
            const counter = elements.walkInRoomGroups?.querySelector(`[data-walkin-room-counter="${cssEscape(group.roomTypeId)}"]`);
            if (counter) {
                counter.textContent = `${selectedForType}/${Number(group.maxSelectableRooms || 0)} đã chọn`;
            }
        });

        if (elements.walkInTotalAmount) {
            elements.walkInTotalAmount.textContent = formatMoney(total);
        }
        if (elements.walkInSelectedCount) {
            elements.walkInSelectedCount.textContent = String(state.selectedWalkInRooms.size);
        }
        if (elements.walkInSelectedCapacity) {
            elements.walkInSelectedCapacity.textContent = String(selectedCapacity);
        }
        if (elements.walkInSelectedTypes) {
            elements.walkInSelectedTypes.textContent = selectedTypes.length > 0
                ? selectedTypes.join(" · ")
                : "Chưa chọn phòng";
        }
        if (elements.walkInStayNights) {
            elements.walkInStayNights.textContent = String(state.walkInAvailability?.nights || 0);
        }
        if (elements.walkInPaymentDue) {
            elements.walkInPaymentDue.textContent = formatMoney(payableTotal);
        }
        if (elements.walkInChangeAmount) {
            const change = Math.max(getCurrencyInputValue(elements.walkInPaymentAmount) - payableTotal, 0);
            elements.walkInChangeAmount.textContent = formatMoney(change);
        }
        if (elements.walkInGoPaymentBtn) {
            elements.walkInGoPaymentBtn.disabled = total <= 0;
        }
        if (elements.walkInConfirmBtn) {
            const method = getWalkInPaymentMethod();
            elements.walkInConfirmBtn.disabled = total <= 0 ||
                (method === "cash" && getCurrencyInputValue(elements.walkInPaymentAmount) + 0.01 < payableTotal);
        }
    }

    function markWalkInRoomTypeLocked(roomTypeId, locked) {
        const section = elements.walkInRoomGroups?.querySelector(`[data-walkin-room-type-section="${cssEscape(roomTypeId)}"]`);
        section?.classList.toggle("is-limit-reached", locked);
    }

    async function showWalkInPaymentStep() {
        if (state.selectedWalkInRooms.size === 0) {
            showAlert("Chọn ít nhất một phòng trống trước khi thanh toán.", "warning");
            return;
        }

        try {
            await refreshWalkInPromotionPreview();
        } catch (error) {
            showAlert(error.message, "danger");
            return;
        }

        if (elements.walkInPaymentAmount) {
            setCurrencyInputValue(elements.walkInPaymentAmount, getWalkInPayableTotal());
        }
        showWalkInView("payment");
    }

    async function showWalkInConfirmStep() {
        const total = getWalkInTotal();
        const method = getWalkInPaymentMethod();
        if (total <= 0 || state.selectedWalkInRooms.size === 0) {
            showAlert("Chọn ít nhất một phòng trống.", "warning");
            return;
        }

        try {
            await refreshWalkInPromotionPreview();
        } catch (error) {
            showAlert(error.message, "danger");
            return;
        }

        const payableTotal = getWalkInPayableTotal();
        if (method === "cash" && getCurrencyInputValue(elements.walkInPaymentAmount) + 0.01 < payableTotal) {
            showAlert(`Khách cần đưa đủ ${formatMoney(payableTotal)} trước khi xác nhận.`, "warning");
            return;
        }

        renderWalkInConfirmSummary();
        showWalkInView("confirm");
    }

    function updateWalkInPaymentUi() {
        const method = getWalkInPaymentMethod();
        if (elements.walkInPaymentMethod) {
            elements.walkInPaymentMethod.value = method === "vnpay" ? "VNPAY" : "Tiền mặt";
        }
        if (elements.walkInCashBox) {
            elements.walkInCashBox.hidden = method !== "cash";
        }
        if (elements.walkInReviewBtn) {
            elements.walkInReviewBtn.textContent = method === "vnpay" ? "Xác nhận chuyển VNPay" : "Xem xác nhận";
        }
        updateWalkInSelectionState();
    }

    function getWalkInPaymentMethod() {
        const checked = Array.from(elements.walkInPaymentChoices || []).find(function (item) {
            return item.checked;
        });
        return checked?.value || "cash";
    }

    function renderWalkInConfirmSummary() {
        if (!elements.walkInConfirmSummary) return;

        const rooms = Array.from(state.selectedWalkInRooms.values())
            .sort(function (a, b) {
                return Number(a.floor || 0) - Number(b.floor || 0) ||
                    String(a.roomNumber).localeCompare(String(b.roomNumber), "vi", { numeric: true });
            });
        const total = getWalkInTotal();
        const discountAmount = getWalkInDiscountAmount();
        const payableTotal = getWalkInPayableTotal();
        const method = getWalkInPaymentMethod();
        const paid = method === "vnpay" ? 0 : getCurrencyInputValue(elements.walkInPaymentAmount);
        const change = Math.max(paid - payableTotal, 0);
        const nights = state.walkInAvailability?.nights || 0;
        const guestCount = Number(elements.walkInGuestCount?.value || 1);
        const roomLines = Array.from(rooms.reduce(function (lines, room) {
            const key = `${room.roomTypeName || "Phòng"}|${Number(room.pricePerNight || 0)}`;
            if (!lines.has(key)) {
                lines.set(key, {
                    roomTypeName: formatDisplayText(room.roomTypeName || "Phòng"),
                    pricePerNight: Number(room.pricePerNight || 0),
                    rooms: []
                });
            }

            lines.get(key).rooms.push(room);
            return lines;
        }, new Map()).values());
        const roomsCount = rooms.length;
        const customerName = formatPersonName(elements.walkInCustomerName?.value.trim() || "Khách vãng lai");
        const phone = elements.walkInPhone?.value.trim() || "Chưa có SĐT";
        const identity = elements.walkInIdentity?.value.trim() || "Chưa có CCCD/Hộ chiếu";
        const paymentLabel = method === "vnpay" ? "VNPay" : "Tiền mặt";
        const checkInLabel = formatDateLabel(elements.walkInCheckinDate?.value || "");
        const checkOutLabel = formatDateLabel(elements.walkInCheckoutDate?.value || "");

        elements.walkInConfirmSummary.innerHTML = `
            <article class="walkin-invoice-card">
                <header class="walkin-invoice-header">
                    <div>
                        <h3 class="text-white m-0">Chi tiết thanh toán</h3>
                    </div>
                    <span class="text-white">${escapeHtml(paymentLabel)}</span>
                </header>
                <div class="walkin-invoice-body">
                    <div class="walkin-invoice-guest">
                        <div>
                            <span>Khách hàng</span>
                            <strong>${escapeHtml(customerName)}</strong>
                            <small>${escapeHtml(phone)} · ${escapeHtml(identity)} · ${guestCount} khách</small>
                        </div>
                        <div>
                            <span>Phòng</span>
                            <strong>${rooms.map(function (room) { return escapeHtml(formatDisplayText(room.roomNumber)); }).join(", ")}</strong>
                            <small>${roomsCount} phòng · ${nights} đêm</small>
                        </div>
                    </div>

                    <div class="walkin-invoice-lines">
                        ${roomLines.map(function (line) {
                            const lineTotal = line.pricePerNight * line.rooms.length * nights;
                            const roomNumbers = line.rooms
                                .map(function (room) { return formatDisplayText(room.roomNumber); })
                                .sort(function (a, b) { return String(a).localeCompare(String(b), "vi", { numeric: true }); })
                                .join(", ");
                            return `
                                <div class="walkin-invoice-line">
                                    <div>
                                        <strong>${escapeHtml(formatDisplayText(line.roomTypeName))} x ${line.rooms.length} phòng</strong>
                                        <span>Phòng ${escapeHtml(roomNumbers)} · ${formatMoney(line.pricePerNight)} / đêm</span>
                                    </div>
                                    <strong>${formatMoney(lineTotal)}</strong>
                                </div>
                            `;
                        }).join("")}
                    </div>

                    <div class="walkin-invoice-dates">
                        <div>
                            <span>Nhận phòng</span>
                            <strong>${escapeHtml(checkInLabel)}</strong>
                            <small>Từ 14:00</small>
                        </div>
                        <div>
                            <span>Trả phòng</span>
                            <strong>${escapeHtml(checkOutLabel)}</strong>
                            <small>Trước 12:00</small>
                        </div>
                    </div>

                    <div class="walkin-invoice-money">
                        <div class="walkin-invoice-row">
                            <span>Giá phòng (${roomsCount} phòng x ${nights} đêm)</span>
                            <strong>${formatMoney(total)}</strong>
                        </div>
                        ${discountAmount > 0 ? `
                            <div class="walkin-invoice-row walkin-invoice-discount">
                                <span>Khuyến mãi${state.walkInPromotionPreview?.promotionCode ? " " + escapeHtml(state.walkInPromotionPreview.promotionCode) : ""}</span>
                                <strong>-${formatMoney(discountAmount)}</strong>
                            </div>
                        ` : ""}
                        ${method === "cash" ? `
                            <div class="walkin-invoice-row">
                                <span>Khách đưa</span>
                                <strong>${formatMoney(paid)}</strong>
                            </div>
                            <div class="walkin-invoice-row">
                                <span>Tiền thối</span>
                                <strong>${formatMoney(change)}</strong>
                            </div>
                        ` : ""}
                        <div class="walkin-invoice-total">
                            <span>Cần thanh toán</span>
                            <strong>${formatMoney(payableTotal)}</strong>
                        </div>
                    </div>

                    <div class="walkin-invoice-note">
                        <i class="bi bi-shield-check"></i>
                        <span>${method === "vnpay" ? "Sau khi hoàn tất, hệ thống sẽ chuyển sang cổng VNPay để thanh toán." : "Kiểm tra số tiền khách đưa và tiền thối trước khi hoàn tất check-in."}</span>
                    </div>
                </div>
            </article>
        `;
    }

    async function submitWalkInCheckIn() {
        let total = getWalkInPayableTotal();
        if (!elements.walkInCustomerName?.value.trim()) {
            showAlert("Nhập họ tên khách vãng lai.", "warning");
            return;
        }
        if (!state.walkInAvailability || state.selectedWalkInRooms.size === 0) {
            showAlert("Chọn ít nhất một phòng trống.", "warning");
            return;
        }
        const method = getWalkInPaymentMethod();
        try {
            await refreshWalkInPromotionPreview();
            total = getWalkInPayableTotal();
        } catch (error) {
            showAlert(error.message, "danger");
            return;
        }
        if (method === "cash" && getCurrencyInputValue(elements.walkInPaymentAmount) + 0.01 < total) {
            showAlert(`Khách cần thanh toán đủ ${formatMoney(total)} trước khi check-in.`, "warning");
            return;
        }

        setButtonBusy(elements.walkInConfirmBtn, true, method === "vnpay" ? "Đang tạo VNPay..." : "Đang check-in...");
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
                    checkInDate: elements.walkInCheckinDate?.value || "",
                    checkOutDate: elements.walkInCheckoutDate?.value || "",
                    guestCount: Number(elements.walkInGuestCount?.value || 1),
                    roomIds: Array.from(state.selectedWalkInRooms.keys()),
                    paymentAmount: method === "vnpay" ? 0 : getCurrencyInputValue(elements.walkInPaymentAmount),
                    paymentMethod: elements.walkInPaymentMethod?.value || "",
                    note: elements.walkInNote?.value || ""
                })
            });

            if (!result.success) {
                showAlert(result.message || "Check-in vãng lai thất bại.", "danger");
                return;
            }

            if (result.requiresOnlinePayment && result.paymentUrl) {
                showAlert("Đã giữ phòng đã chọn. Đang chuyển sang VNPay.", "info");
                window.location.href = result.paymentUrl;
                return;
            }

            renderWalkInDone({
                bookingCode: result.bookingCode,
                message: `${result.message} Mã đặt phòng ${result.bookingCode}. Đã thu ${formatMoney(result.grandTotal)}.`,
                rooms: result.assignedRooms || [],
                totalAmount: result.grandTotal
            });
            showWalkInView("done");
            showAlert(`${result.message} Mã đặt phòng ${result.bookingCode}, đã thu ${formatMoney(result.grandTotal)}.`, "success");
        } catch (error) {
            showAlert(error.message, "danger");
        } finally {
            setButtonBusy(elements.walkInConfirmBtn, false);
        }
    }

    function renderWalkInDone(options) {
        const booking = options.booking || {};
        const rooms = options.rooms || booking.assignedRooms || [];
        const totalAmount = Number(options.totalAmount ?? booking.totalAmount ?? booking.paidAmount ?? 0);
        const checkInDate = booking.checkInDate || formatDateLabel(elements.walkInCheckinDate?.value || "");
        const checkOutDate = booking.checkOutDate || formatDateLabel(elements.walkInCheckoutDate?.value || "");
        const nights = Number(booking.nights || state.walkInAvailability?.nights || 0);
        const groupedRooms = Array.from(rooms.reduce(function (groups, room) {
            const typeName = formatDisplayText(room.roomTypeName || "Phòng");
            if (!groups.has(typeName)) {
                groups.set(typeName, []);
            }

            groups.get(typeName).push(room);
            return groups;
        }, new Map()).entries())
            .sort(function (a, b) {
                return a[0].localeCompare(b[0], "vi", { numeric: true });
            });

        if (elements.walkInDoneMessage) {
            elements.walkInDoneMessage.textContent = options.message || "Check-in khách vãng lai thành công.";
        }

        if (!elements.walkInDoneRooms) {
            return;
        }

        elements.walkInDoneRooms.innerHTML = `
            <div class="walkin-done-meta">
                <div>
                    <span>Mã đặt phòng</span>
                    <strong>${escapeHtml(options.bookingCode || booking.bookingCode || "")}</strong>
                </div>
                <div>
                    <span>Thời gian ở</span>
                    <strong>${escapeHtml(checkInDate || "-")} - ${escapeHtml(checkOutDate || "-")}</strong>
                    <small>${nights > 0 ? `${nights} đêm` : "Đã xác nhận"}</small>
                </div>
                <div>
                    <span>Đã thanh toán</span>
                    <strong>${formatMoney(totalAmount)}</strong>
                </div>
            </div>
            <div class="walkin-done-room-block">
                <div class="walkin-done-room-heading">
                    <span>Phòng khách đã nhận</span>
                    <strong>${rooms.length} phòng</strong>
                </div>
                ${rooms.length > 0 ? `
                    <div class="walkin-done-room-list">
                        ${groupedRooms.map(function ([typeName, items]) {
                            const sortedRooms = items.slice().sort(function (a, b) {
                                return String(a.roomNumber).localeCompare(String(b.roomNumber), "vi", { numeric: true });
                            });

                            return `
                                <section class="walkin-done-room-group">
                                    <div>
                                        <strong>${escapeHtml(typeName)} x${items.length}</strong>
                                        <span>${sortedRooms.map(function (room) { return `Phòng ${escapeHtml(formatDisplayText(room.roomNumber))}`; }).join(" · ")}</span>
                                    </div>
                                    <div class="walkin-done-room-chips">
                                        ${sortedRooms.map(function (room) {
                                            return `<span>${escapeHtml(formatDisplayText(room.roomNumber))}</span>`;
                                        }).join("")}
                                    </div>
                                </section>
                            `;
                        }).join("")}
                    </div>
                ` : `<div class="walkin-done-empty">Chưa tải được danh sách phòng. Mã đặt phòng đã được ghi nhận.</div>`}
            </div>
        `;
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
                    return `${item.requiredRooms} ${escapeHtml(formatRoomTypeName(item.roomTypeName))}`;
                })
                .join(" · ");
            const statusClass = booking.canCheckIn ? "ready" : "blocked";
            const buttonText = booking.canCheckIn ? "Check-in" : "Xem chi tiết";

            return `
                <article class="arrival-card ${statusClass}" data-arrival-code="${escapeHtml(booking.bookingCode)}">
                    <div class="arrival-main">
                        <div class="arrival-code">${escapeHtml(booking.bookingCode)}</div>
                        <h3>${escapeHtml(formatPersonName(booking.customerName))}</h3>
                        <p>${escapeHtml(formatSentenceText(booking.phoneNumber || "Chưa có số điện thoại"))}</p>
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
            state.roomMapServiceUsage = null;
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
                return `<span class="room-map-count ${escapeHtml(item.status)}">${escapeHtml(formatSentenceText(item.statusLabel))}: <strong>${item.count}</strong></span>`;
            }).join("");
        }

        const floors = state.roomMap.floors || [];
        renderRoomMapTypeFilter(floors);
        const selectedRoom = findRoomMapRoom(state.selectedRoomMapRoomId);
        if (state.selectedRoomMapRoomId && !selectedRoom) {
            state.selectedRoomMapRoomId = "";
            closeRoomMapDetailModal();
        }
        if (!elements.roomMapFloors) return;
        if (floors.length === 0) {
            elements.roomMapFloors.innerHTML = `<div class="arrival-empty">Chưa có phòng trong hệ thống.</div>`;
            return;
        }

        const filter = state.roomMapFilter || "all";
        const typeFilter = state.roomMapTypeFilter || "all";
        const html = floors.map(function (floor) {
            const rooms = (floor.rooms || []).filter(function (room) {
                const matchesStatus = filter === "all" || room.status === filter;
                const matchesType = typeFilter === "all" || room.roomTypeName === typeFilter;
                return matchesStatus && matchesType;
            }).sort(function (a, b) {
                return String(a.roomNumber).localeCompare(String(b.roomNumber), "vi", { numeric: true });
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

    function renderRoomMapTypeFilter(floors) {
        if (!elements.roomMapTypeFilter) return;

        const currentValue = state.roomMapTypeFilter || elements.roomMapTypeFilter.value || "all";
        const roomTypes = Array.from((floors || []).reduce(function (types, floor) {
            (floor.rooms || []).forEach(function (room) {
                if (room.roomTypeName) {
                    types.add(room.roomTypeName);
                }
            });
            return types;
        }, new Set()))
            .sort(function (a, b) {
                return a.localeCompare(b, "vi", { numeric: true });
            });

        elements.roomMapTypeFilter.innerHTML = `<option value="all">Tất cả</option>` +
            roomTypes.map(function (roomTypeName) {
                return `<option value="${escapeHtml(roomTypeName)}">${escapeHtml(formatDisplayText(roomTypeName))}</option>`;
            }).join("");
        elements.roomMapTypeFilter.value = roomTypes.includes(currentValue) ? currentValue : "all";
        state.roomMapTypeFilter = elements.roomMapTypeFilter.value;
    }

    function renderRoomMapTile(room) {
        const checkout = room.checkOutDate
            ? `<small>Trả ${escapeHtml(room.checkOutDate)}</small>`
            : `<small class="room-map-empty-line" aria-hidden="true">&nbsp;</small>`;

        return `
            <article class="room-map-tile ${escapeHtml(room.status)} ${state.selectedRoomMapRoomId === room.roomId ? "selected" : ""}"
                data-room-map-room-id="${escapeHtml(room.roomId)}"
                role="button"
                tabindex="0">
                <div class="room-map-tile-head">
                    <strong>${escapeHtml(formatDisplayText(room.roomNumber))}</strong>
                </div>
                <div class="room-map-tile-meta">
                    <span>${escapeHtml(formatSentenceText(room.statusLabel))}</span>
                    ${checkout}
                    <small>${escapeHtml(formatDisplayText(room.roomTypeName))}</small>
                </div>
            </article>
        `;
    }

    async function selectRoomMapRoom(roomId) {
        const room = findRoomMapRoom(roomId);
        if (!room) return;

        state.selectedRoomMapRoomId = room.roomId;
        renderRoomMap();
        renderRoomMapDetail(room, { loadingServices: Boolean(room.bookingCode) });

        if (!room.bookingCode) {
            return;
        }

        try {
            const [serviceUsage, bookingLookup] = await Promise.all([
                getRoomMapServiceUsage(),
                fetchJson(`/Receptionist/Lookup?code=${encodeURIComponent(room.bookingCode)}`)
            ]);
            if (state.selectedRoomMapRoomId !== room.roomId) {
                return;
            }

            const stay = (serviceUsage.activeStays || []).find(function (item) {
                return item.bookingCode === room.bookingCode;
            }) || null;
            renderRoomMapDetail(room, {
                stay,
                booking: bookingLookup.success ? bookingLookup.booking : null
            });
        } catch {
            if (state.selectedRoomMapRoomId === room.roomId) {
                renderRoomMapDetail(room, { serviceError: true });
            }
        }
    }

    function findRoomMapRoom(roomId) {
        if (!roomId || !state.roomMap) return null;

        for (const floor of state.roomMap.floors || []) {
            const room = (floor.rooms || []).find(function (item) {
                return item.roomId === roomId;
            });
            if (room) return room;
        }

        return null;
    }

    async function getRoomMapServiceUsage() {
        if (state.roomMapServiceUsage) {
            return state.roomMapServiceUsage;
        }

        state.roomMapServiceUsage = await fetchJson("/Receptionist/ServiceUsage");
        return state.roomMapServiceUsage;
    }

    function renderRoomMapDetail(room, options = {}) {
        if (!elements.roomMapDetailModal || !elements.roomMapDetailBody) return;

        if (!room) {
            closeRoomMapDetailModal();
            return;
        }

        const stay = options.stay || null;
        const booking = options.booking || null;
        const serviceLines = stay?.serviceLines || [];
        const serviceHtml = options.loadingServices
            ? `<div class="room-map-service-empty">Đang tải dịch vụ...</div>`
            : options.serviceError
                ? `<div class="room-map-service-empty">Chưa tải được dịch vụ của phòng này.</div>`
                : serviceLines.length === 0
                    ? `<div class="room-map-service-empty">Chưa phát sinh dịch vụ.</div>`
                    : serviceLines.map(function (line) {
                        return `
                            <div class="room-map-service-line">
                                <span>${escapeHtml(formatDisplayText(line.serviceName))} x${line.quantity}</span>
                                <strong>${formatMoney(line.total)}</strong>
                            </div>
                        `;
                    }).join("");
        const bookedRooms = booking?.assignedRooms?.length
            ? booking.assignedRooms.map(function (item) { return formatDisplayText(item.roomNumber); })
            : (stay?.roomNumbers || []).map(formatDisplayText);
        const roomNumbers = bookedRooms.length
            ? `Phòng ${bookedRooms.map(escapeHtml).join(", ")}`
            : `Phòng ${escapeHtml(formatDisplayText(room.roomNumber))}`;
        const canSetMaintenance = room.status === "available";
        const canCancelMaintenance = room.status === "maintenance";
        const maintenanceActionHtml = canSetMaintenance || canCancelMaintenance
            ? `
                <div class="room-map-maintenance-actions">
                    <button type="button"
                            class="room-map-maintenance-btn ${canSetMaintenance ? "to-maintenance" : "to-available"}"
                            data-room-map-maintenance
                            data-room-id="${escapeHtml(room.roomId)}"
                            data-maintenance="${canSetMaintenance ? "true" : "false"}">
                        <i class="bi ${canSetMaintenance ? "bi-tools" : "bi-check2-circle"}"></i>
                        ${canSetMaintenance ? "Bảo trì phòng" : "Hủy bảo trì"}
                    </button>
                </div>
            `
            : "";
        const guestHtml = room.status === "occupied"
            ? `
                <div class="room-map-guest-box">
                    <div class="room-map-section-title">
                        <span>Thông tin khách hàng</span>
                    </div>
                    <div class="room-map-detail-grid">
                        <div>
                            <span>Tên khách hàng</span>
                            <strong>${escapeHtml(formatPersonName(booking?.customerName || stay?.customerName || room.currentGuestName || "-"))}</strong>
                        </div>
                        <div>
                            <span>CCCD/Hộ chiếu</span>
                            <strong>${escapeHtml(booking?.identityNumber || "-")}</strong>
                        </div>
                        <div>
                            <span>Số điện thoại</span>
                            <strong>${escapeHtml(formatSentenceText(booking?.phoneNumber || stay?.phoneNumber || "-"))}</strong>
                        </div>
                        <div>
                            <span>Ngày đặt</span>
                            <strong>${escapeHtml(booking?.bookingDate || "-")}</strong>
                        </div>
                        <div>
                            <span>Ngày trả</span>
                            <strong>${escapeHtml(booking?.checkOutDate || stay?.checkOutDate || room.checkOutDate || "-")}</strong>
                        </div>
                        <div>
                            <span>Mã đặt phòng</span>
                            <strong>${escapeHtml(booking?.bookingCode || room.bookingCode || "-")}</strong>
                        </div>
                        <div class="room-map-detail-wide">
                            <span>Các phòng đang được booking cùng</span>
                            <strong>${roomNumbers}</strong>
                        </div>
                    </div>
                </div>
            `
            : "";

        elements.roomMapDetailBody.innerHTML = `
            <div class="room-map-detail-head">
                <div>
                    <h3>Phòng ${escapeHtml(formatDisplayText(room.roomNumber))}</h3>
                </div>
                <span class="room-map-detail-status ${escapeHtml(room.status)}">${escapeHtml(formatSentenceText(room.statusLabel))}</span>
            </div>
            <div class="room-map-detail-grid">
                <div>
                    <span>Loại phòng</span>
                    <strong>${escapeHtml(formatDisplayText(room.roomTypeName))}</strong>
                </div>
                <div>
                    <span>Tình trạng phòng</span>
                    <strong>${escapeHtml(formatSentenceText(room.statusLabel))}</strong>
                </div>
            </div>
            ${guestHtml}
            <div class="room-map-service-box">
                <div class="room-map-service-title">
                    <span>Dịch vụ đã sử dụng</span>
                </div>
                ${serviceHtml}
            </div>
            ${maintenanceActionHtml}
        `;
        elements.roomMapDetailModal.hidden = false;
    }

    function closeRoomMapDetailModal() {
        if (elements.roomMapDetailModal) {
            elements.roomMapDetailModal.hidden = true;
        }
        if (elements.roomMapDetailBody) {
            elements.roomMapDetailBody.innerHTML = "";
        }
        state.selectedRoomMapRoomId = "";
        if (state.currentStep === "roomMap") {
            renderRoomMap();
        }
    }

    async function updateRoomMaintenance(button) {
        const roomId = button.dataset.roomId || "";
        const maintenance = button.dataset.maintenance === "true";
        if (!roomId) return;

        const confirmed = confirm(maintenance
            ? "Chuyển phòng này sang trạng thái bảo trì?"
            : "Hủy bảo trì và chuyển phòng về trạng thái trống?");
        if (!confirmed) return;

        setButtonBusy(button, true, "Đang cập nhật...");
        try {
            const result = await fetchJson("/Receptionist/RoomMaintenance", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ roomId, maintenance })
            });
            if (!result.success) {
                showAlert(result.message || "Không thể cập nhật trạng thái phòng.", "danger");
                setButtonBusy(button, false);
                return;
            }

            showAlert(result.message || "Đã cập nhật trạng thái phòng.", "success");
            state.roomMapServiceUsage = null;
            closeRoomMapDetailModal();
            await loadRoomMap();
        } catch (error) {
            showAlert(error.message || "Không thể cập nhật trạng thái phòng.", "danger");
            setButtonBusy(button, false);
        }
    }

    async function loadServiceUsage(preferredBookingCode) {
        const preferredCode = typeof preferredBookingCode === "string"
            ? preferredBookingCode
            : resolveServiceBookingCode();

        setStep("services");

        try {
            const result = await fetchJson("/Receptionist/ServiceUsage");
            if (!result.success) {
                showAlert(result.message || "Không tải được dữ liệu dịch vụ.", "danger");
                return;
            }

            state.serviceUsage = result;
            renderServiceUsage(preferredCode);
            showAlert("Đã tải dữ liệu ghi nhận dịch vụ.", "info");
        } catch (error) {
            showAlert(error.message, "danger");
        }
    }

    function renderServiceUsage(preferredBookingCode) {
        const activeStays = state.serviceUsage?.activeStays || [];
        const services = state.serviceUsage?.services || [];

        renderServiceSelects(activeStays, services, preferredBookingCode);
        renderServiceSelection();
    }

    function renderServiceSelects(activeStays, services, preferredBookingCode) {
        if (elements.serviceBookingSelect) {
            const bookingOptions = activeStays.map(function (stay) {
                return {
                    value: stay.bookingCode,
                    text: formatServiceStayOptionValue(stay),
                    data: stay
                };
            });

            if (!tomBookingSelect) {
                tomBookingSelect = new TomSelect(elements.serviceBookingSelect, {
                    valueField: 'value',
                    labelField: 'text',
                    searchField: ['text', 'value'],
                    options: bookingOptions,
                    placeholder: "Tìm theo tên khách, phòng hoặc mã đặt phòng...",
                    maxOptions: null,
                    onChange: function() {
                        renderServiceSelection();
                    }
                });
            } else {
                tomBookingSelect.clearOptions();
                tomBookingSelect.addOption(bookingOptions);
                tomBookingSelect.refreshOptions(false);
            }

            const fallbackCode = activeStays[0]?.bookingCode || "";
            const selectedCode = activeStays.some(function(stay) { return stay.bookingCode === preferredBookingCode; }) 
                ? preferredBookingCode : fallbackCode;
            
            if (selectedCode) {
                tomBookingSelect.setValue(selectedCode, true);
            } else {
                tomBookingSelect.clear(true);
            }
            if (activeStays.length === 0) tomBookingSelect.disable();
            else tomBookingSelect.enable();
        }

        if (elements.serviceOptionSelect) {
            const serviceOptions = services.map(function (service) {
                return {
                    value: service.serviceId,
                    text: formatServiceOptionValue(service),
                    category: service.category || "",
                    name: service.serviceName,
                    price: service.unitPrice,
                    unit: service.unit
                };
            });

            if (!tomOptionSelect) {
                tomOptionSelect = new TomSelect(elements.serviceOptionSelect, {
                    valueField: 'value',
                    labelField: 'text',
                    searchField: ['text', 'category', 'name'],
                    options: serviceOptions,
                    placeholder: "Tìm theo tên dịch vụ hoặc loại dịch vụ...",
                    maxOptions: null,
                    onChange: function() {
                        updateServiceEstimate();
                    }
                });
            } else {
                tomOptionSelect.clearOptions();
                tomOptionSelect.addOption(serviceOptions);
                tomOptionSelect.refreshOptions(false);
            }

            if (services.length > 0) {
                tomOptionSelect.setValue(services[0].serviceId, true);
                tomOptionSelect.enable();
            } else {
                tomOptionSelect.clear(true);
                tomOptionSelect.disable();
            }
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
        const selectedCode = resolveServiceBookingCode();
        const selectedStay = (state.serviceUsage?.activeStays || []).find(function (stay) {
            return stay.bookingCode === selectedCode;
        }) || null;
        renderSelectedServiceStay(selectedStay);
        updateServiceEstimate();
    }

    function formatServiceStayOptionValue(stay) {
        if (!stay) return "";

        const rooms = (stay.roomNumbers || []).length > 0
            ? ` - phòng ${stay.roomNumbers.map(formatDisplayText).join(", ")}`
            : "";
        return `${stay.bookingCode} - ${formatPersonName(stay.customerName)}${rooms}`;
    }

    function formatServiceOptionValue(service) {
        if (!service) return "";

        return `${formatDisplayText(service.serviceName)} - ${formatMoney(service.unitPrice)}/${formatSentenceText(service.unit)}`;
    }

    function resolveServiceBookingCode() {
        return tomBookingSelect ? tomBookingSelect.getValue() : "";
    }

    function resolveServiceOptionId() {
        return tomOptionSelect ? tomOptionSelect.getValue() : "";
    }

    function getSelectedServiceOptionElement() {
        if (!tomOptionSelect) return null;
        const value = tomOptionSelect.getValue();
        if (!value) return null;
        return tomOptionSelect.options[value];
    }

    function findDatalistOption(datalist, value) {
        return Array.from(datalist?.options || []).find(function (option) {
            return option.value === value;
        }) || null;
    }

    function normalizeSearchText(value) {
        return String(value ?? "")
            .trim()
            .toLocaleLowerCase("vi-VN")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .replace(/đ/g, "d");
    }

    function renderSelectedServiceStay(stay) {
        if (!elements.serviceSelectedStayInfo) return;

        if (!stay) {
            elements.serviceSelectedStayInfo.innerHTML = `<span>Chưa chọn khách / phòng.</span>`;
            return;
        }

        const rooms = (stay.roomNumbers || []).length > 0
            ? `Phòng ${stay.roomNumbers.map(function (roomNumber) { return escapeHtml(formatDisplayText(roomNumber)); }).join(", ")}`
            : "Chưa gán phòng";
        elements.serviceSelectedStayInfo.innerHTML = `
            <div>
                <span>Đang ghi nhận cho</span>
                <strong>${escapeHtml(formatPersonName(stay.customerName))}</strong>
                <small>${escapeHtml(stay.bookingCode)} · ${rooms} · Trả ${escapeHtml(stay.checkOutDate)}</small>
            </div>
            <strong>${formatMoney(stay.serviceTotal || 0)}</strong>
        `;
    }

    function updateServiceEstimate() {
        if (!elements.serviceLineEstimate) return;

        const option = getSelectedServiceOptionElement();
        const price = Number(option?.price || 0);
        const quantity = Math.max(Number(elements.serviceQuantityInput?.value || 0), 0);
        elements.serviceLineEstimate.textContent = formatMoney(price * quantity);
        if (elements.serviceSelectedServiceInfo) {
            const serviceName = option?.name || "Chưa chọn dịch vụ";
            const unit = option?.unit || "";
            const category = option?.category || "Dịch vụ";
            elements.serviceSelectedServiceInfo.innerHTML = `
                <span>${escapeHtml(category)}</span>
                <strong>${escapeHtml(serviceName)}</strong>
                <small>${formatMoney(price)} / ${escapeHtml(unit)} · Số lượng ${quantity || 0}</small>
            `;
        }
    }

    async function addServiceUsage(event) {
        event.preventDefault();

        const bookingCode = resolveServiceBookingCode();
        const serviceId = resolveServiceOptionId();
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

            state.roomMapServiceUsage = null;
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
        showCheckoutListPage();

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
        const visibleStays = getVisibleCheckoutStays(stays);
        if (elements.checkoutSummary) {
            elements.checkoutSummary.textContent = state.checkoutScope === "today"
                ? `${visibleStays.length}/${stays.length} khách trả hôm nay`
                : `${visibleStays.length}/${stays.length} khách đang ở`;
        }

        if (!elements.checkoutStayList) return;
        if (stays.length === 0) {
            elements.checkoutStayList.innerHTML = `<div class="arrival-empty">Hiện chưa có khách đang lưu trú để check-out.</div>`;
            state.selectedCheckoutCode = "";
            renderCheckoutDetail(null);
            return;
        }

        if (visibleStays.length === 0) {
            elements.checkoutStayList.innerHTML = `<div class="arrival-empty">Không tìm thấy khách phù hợp.</div>`;
            state.selectedCheckoutCode = "";
            renderCheckoutDetail(null);
            return;
        }

        const selectedCode = visibleStays.some(function (stay) {
            return stay.bookingCode === preferredBookingCode;
        }) ? preferredBookingCode : "";
        state.selectedCheckoutCode = selectedCode || "";

        elements.checkoutStayList.innerHTML = visibleStays.map(function (stay) {
            const rooms = (stay.roomNumbers || []).length > 0
                ? `Phòng ${stay.roomNumbers.map(function (roomNumber) { return escapeHtml(formatDisplayText(roomNumber)); }).join(", ")}`
                : "Chưa gán phòng";
            const remainingClass = stay.remainingAmount > 0 ? "due" : "paid";
            const isTodayCheckout = isCheckoutToday(stay);
            const todayBadge = isTodayCheckout
                ? `<span class="checkout-today-badge">Trả hôm nay</span>`
                : "";

            return `
                <article class="checkout-stay-card ${remainingClass} ${isTodayCheckout ? "today-checkout" : ""} ${stay.bookingCode === selectedCode ? "selected" : ""}" data-checkout-stay="${escapeHtml(stay.bookingCode)}">
                    <div class="service-stay-main">
                        <div class="checkout-card-meta">
                            <span class="arrival-code">${escapeHtml(stay.bookingCode)}</span>
                            ${todayBadge}
                        </div>
                        <h3>${escapeHtml(formatPersonName(stay.customerName))}</h3>
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
                renderCheckoutDetail(getSelectedCheckoutStay());
                showCheckoutPaymentPage();
            });
        });

        if (elements.checkoutDetailPanel?.hidden === false && state.selectedCheckoutCode) {
            renderCheckoutDetail(getSelectedCheckoutStay());
        }
    }

    function showCheckoutListPage() {
        if (elements.checkoutListPanel) elements.checkoutListPanel.hidden = false;
        if (elements.checkoutDetailPanel) elements.checkoutDetailPanel.hidden = true;
        if (elements.checkoutPaymentResultPanel) elements.checkoutPaymentResultPanel.hidden = true;
    }

    function showCheckoutPaymentPage() {
        if (elements.checkoutListPanel) elements.checkoutListPanel.hidden = true;
        if (elements.checkoutDetailPanel) elements.checkoutDetailPanel.hidden = false;
        if (elements.checkoutPaymentResultPanel) elements.checkoutPaymentResultPanel.hidden = true;
        elements.checkoutDetailPanel?.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    function showCheckoutPaymentResultPage() {
        if (elements.checkoutListPanel) elements.checkoutListPanel.hidden = true;
        if (elements.checkoutDetailPanel) elements.checkoutDetailPanel.hidden = true;
        if (elements.checkoutPaymentResultPanel) elements.checkoutPaymentResultPanel.hidden = false;
        elements.checkoutPaymentResultPanel?.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    function renderCheckoutPaymentResult(result) {
        const success = result.status === "success";
        const cancelled = result.status === "cancelled";
        elements.checkoutPaymentResultPanel?.classList.toggle("is-success", success);
        elements.checkoutPaymentResultPanel?.classList.toggle("is-error", !success);
        if (elements.checkoutPaymentResultIcon) {
            elements.checkoutPaymentResultIcon.innerHTML = `<i class="bi ${success ? "bi-check2" : "bi-x-lg"}"></i>`;
        }
        if (elements.checkoutPaymentResultTitle) {
            elements.checkoutPaymentResultTitle.textContent = success
                ? "Thanh toán check-out thành công"
                : cancelled
                    ? "Thanh toán VNPay đã hủy"
                    : "Thanh toán check-out chưa hoàn tất";
        }
        if (elements.checkoutPaymentResultMessage) {
            elements.checkoutPaymentResultMessage.textContent = result.message || (success
                ? "VNPay đã xác nhận thanh toán. Hóa đơn đã được cập nhật và phòng đã được trả."
                : "VNPay chưa xác nhận thanh toán. Khách vẫn còn trong danh sách đang ở.");
        }
        if (elements.checkoutPaymentResultBooking) {
            elements.checkoutPaymentResultBooking.textContent = result.bookingCode || "-";
        }
        if (elements.checkoutPaymentResultAmount) {
            elements.checkoutPaymentResultAmount.textContent = formatMoney(result.amount || 0);
        }
        if (elements.checkoutPaymentResultTransaction) {
            elements.checkoutPaymentResultTransaction.textContent = result.transactionNo || "Chưa có";
        }
        if (elements.checkoutPaymentResultBank) {
            elements.checkoutPaymentResultBank.textContent = result.bankCode || "Chưa có";
        }
        showCheckoutPaymentResultPage();
    }

    function getVisibleCheckoutStays(stays) {
        const todayKey = getTodayDateKey();
        const query = normalizeSearchText(state.checkoutSearch || "");
        const scopedStays = stays.filter(function (stay) {
            if (state.checkoutScope !== "today") return true;
            return getCheckoutDateKey(stay) === todayKey;
        });
        const filteredStays = query
            ? scopedStays.filter(function (stay) {
                return normalizeSearchText([
                    stay.bookingCode,
                    stay.customerName,
                    stay.phoneNumber,
                    stay.email,
                    stay.checkInDate,
                    stay.checkOutDate,
                    (stay.roomNumbers || []).join(" ")
                ].join(" ")).includes(query);
            })
            : scopedStays;

        return sortCheckoutStays(filteredStays);
    }

    function sortCheckoutStays(stays) {
        const sorted = [...stays];
        const sortMode = state.checkoutSort || "checkoutDateAsc";
        sorted.sort(function (a, b) {
            if (sortMode === "checkoutDateDesc") {
                return compareText(getCheckoutDateKey(b), getCheckoutDateKey(a)) ||
                    compareText(getFirstCheckoutRoom(a), getFirstCheckoutRoom(b));
            }
            if (sortMode === "roomAsc") {
                return compareText(getFirstCheckoutRoom(a), getFirstCheckoutRoom(b)) ||
                    compareText(getCheckoutDateKey(a), getCheckoutDateKey(b));
            }
            if (sortMode === "nameAsc") {
                return compareText(formatPersonName(a.customerName), formatPersonName(b.customerName)) ||
                    compareText(getCheckoutDateKey(a), getCheckoutDateKey(b));
            }
            if (sortMode === "remainingDesc") {
                return Number(b.remainingAmount || 0) - Number(a.remainingAmount || 0) ||
                    compareText(getCheckoutDateKey(a), getCheckoutDateKey(b));
            }

            return compareText(getCheckoutDateKey(a), getCheckoutDateKey(b)) ||
                compareText(getFirstCheckoutRoom(a), getFirstCheckoutRoom(b));
        });
        return sorted;
    }

    function compareText(left, right) {
        return String(left || "").localeCompare(String(right || ""), "vi", {
            numeric: true,
            sensitivity: "base"
        });
    }

    function getFirstCheckoutRoom(stay) {
        return (stay.roomNumbers || [])[0] || "";
    }

    function isCheckoutToday(stay) {
        return getCheckoutDateKey(stay) === getTodayDateKey();
    }

    function getCheckoutDateKey(stay) {
        return normalizeDateKey(stay?.checkOutDate);
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
            if (elements.checkoutMoneySummary) elements.checkoutMoneySummary.innerHTML = "";
            if (elements.checkoutBookingCode) elements.checkoutBookingCode.value = "";
            setCurrencyInputValue(elements.checkoutPaymentAmount, 0);
            if (elements.checkoutReceiptEmail) elements.checkoutReceiptEmail.value = "";
            if (elements.checkoutSendEmail) elements.checkoutSendEmail.checked = false;
            if (elements.checkoutPrintInvoice) elements.checkoutPrintInvoice.checked = true;
            syncCheckoutEmailField();
            if (elements.confirmCheckoutBtn) elements.confirmCheckoutBtn.disabled = true;
            return;
        }

        if (elements.checkoutDetailPanel) elements.checkoutDetailPanel.hidden = false;
        elements.checkoutDetail.classList.remove("checkout-detail-empty");
        const roomLines = stay.roomLines || [];
        const serviceLines = stay.serviceLines || [];
        const roomsHtml = roomLines.length === 0
            ? `<div class="checkout-line muted">Chưa có dòng phòng.</div>`
            : roomLines.map(function (line) {
                const room = line.roomNumber ? ` · Phòng ${escapeHtml(formatDisplayText(line.roomNumber))}` : "";
                return `
                    <div class="checkout-line">
                        <span>${escapeHtml(formatDisplayText(line.roomTypeName))}${room}</span>
                        <strong>${formatMoney(line.total)}</strong>
                    </div>
                `;
            }).join("");
        const servicesHtml = serviceLines.length === 0
            ? `<div class="checkout-line muted">Chưa phát sinh dịch vụ.</div>`
            : serviceLines.map(function (line) {
                return `
                    <div class="checkout-line">
                        <span>${escapeHtml(formatDisplayText(line.serviceName))} x${line.quantity}</span>
                        <strong>${formatMoney(line.total)}</strong>
                    </div>
                `;
            }).join("");

        elements.checkoutDetail.innerHTML = `
            <div class="checkout-guest-box">
                <div>
                    <span class="arrival-code">${escapeHtml(stay.bookingCode)}</span>
                    <h4>${escapeHtml(formatPersonName(stay.customerName))}</h4>
                    <p>${escapeHtml(formatSentenceText(stay.phoneNumber || "Chưa có số điện thoại"))}</p>
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
        `;

        if (elements.checkoutMoneySummary) {
            elements.checkoutMoneySummary.innerHTML = `
                <h4>Thanh toán</h4>
                ${moneyRow("Tiền phòng", stay.roomTotal)}
                ${moneyRow("Dịch vụ", stay.serviceTotal)}
                ${stay.discountAmount > 0 ? moneyRow("Giảm giá", -stay.discountAmount) : ""}
                ${moneyRow("Tổng hóa đơn", stay.grandTotal, true)}
                ${moneyRow("Đã thanh toán", stay.paidAmount)}
                ${moneyRow("Còn phải thu", stay.remainingAmount, true)}
            `;
        }

        if (elements.checkoutBookingCode) {
            elements.checkoutBookingCode.value = stay.bookingCode;
        }
        if (elements.checkoutPaymentAmount) {
            setCurrencyInputValue(elements.checkoutPaymentAmount, Math.max(stay.remainingAmount || 0, 0));
        }
        if (elements.checkoutNote) {
            elements.checkoutNote.value = "";
        }
        if (elements.checkoutReceiptEmail) {
            elements.checkoutReceiptEmail.value = stay.email || "";
        }
        if (elements.checkoutSendEmail) {
            elements.checkoutSendEmail.checked = Boolean(stay.email);
        }
        if (elements.checkoutPrintInvoice) {
            elements.checkoutPrintInvoice.checked = true;
        }
        syncCheckoutEmailField();
        updateCheckoutPaymentUi();
        if (elements.confirmCheckoutBtn) {
            elements.confirmCheckoutBtn.disabled = false;
        }
    }

    async function submitCheckout(event) {
        event.preventDefault();

        const bookingCode = elements.checkoutBookingCode?.value || state.selectedCheckoutCode;
        const paymentMethod = elements.checkoutPaymentMethod?.value || "";
        const isVnPay = isCheckoutVnPayPayment();
        const paymentAmount = isVnPay
            ? Math.max(Number(getSelectedCheckoutStay()?.remainingAmount || 0), 0)
            : getCurrencyInputValue(elements.checkoutPaymentAmount);
        const note = elements.checkoutNote?.value || "";
        const sendReceiptEmail = Boolean(elements.checkoutSendEmail?.checked);
        const printInvoice = Boolean(elements.checkoutPrintInvoice?.checked);
        const receiptEmail = (elements.checkoutReceiptEmail?.value || "").trim();
        const selectedStay = getSelectedCheckoutStay();

        if (!bookingCode) {
            showAlert("Chọn khách cần check-out trước.", "warning");
            return;
        }

        if (paymentAmount < 0) {
            showAlert("Số tiền thu thêm không hợp lệ.", "warning");
            return;
        }

        if (isVnPay && paymentAmount <= 0) {
            showAlert("Hóa đơn đã thu đủ, không cần thanh toán VNPay.", "warning");
            return;
        }

        setButtonBusy(elements.confirmCheckoutBtn, true, isVnPay ? "Đang tạo VNPay..." : "Đang check-out...");
        let printWindow = null;
        if (printInvoice && selectedStay && !isVnPay) {
            printWindow = window.open("", "_blank", "width=820,height=900");
            if (printWindow) {
                printWindow.document.write("<!doctype html><title>Đang tạo hóa đơn</title><body style=\"font-family:Arial,sans-serif;padding:28px\">Đang tạo hóa đơn...</body>");
                printWindow.document.close();
            } else {
                showAlert("Trình duyệt đang chặn cửa sổ in hóa đơn.", "warning");
            }
        }

        try {
            const result = await fetchJson("/Receptionist/Checkout", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    bookingCode,
                    paymentAmount,
                    paymentMethod,
                    note,
                    sendReceiptEmail,
                    receiptEmail,
                    printInvoice
                })
            });

            if (!result.success) {
                showAlert(result.message || "Check-out thất bại.", "danger");
                return;
            }

            if (result.requiresOnlinePayment && result.paymentUrl) {
                if (selectedStay) {
                    sessionStorage.setItem("vnpay_checkout_stay_" + bookingCode, JSON.stringify(selectedStay));
                }
                showAlert(result.message || "Đang chuyển sang VNPay.", "info");
                window.location.assign(result.paymentUrl);
                return;
            }

            if (printInvoice && selectedStay && printWindow) {
                printCheckoutInvoice(selectedStay, {
                    paymentAmount,
                    paymentMethod,
                    receiptEmail,
                    result
                }, printWindow);
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

    function updateCheckoutPaymentUi() {
        const isVnPay = isCheckoutVnPayPayment();
        const selectedStay = getSelectedCheckoutStay();
        if (isVnPay && elements.checkoutPaymentAmount && selectedStay) {
            setCurrencyInputValue(elements.checkoutPaymentAmount, Math.max(selectedStay.remainingAmount || 0, 0));
        }
        if (elements.checkoutPaymentAmount) {
            elements.checkoutPaymentAmount.disabled = isVnPay;
        }
        if (elements.confirmCheckoutBtn) {
            elements.confirmCheckoutBtn.innerHTML = isVnPay
                ? `<i class="bi bi-credit-card"></i> Thanh toán VNPay`
                : `<i class="bi bi-check2-circle"></i> Xác nhận check-out`;
        }
    }

    function isCheckoutVnPayPayment() {
        return String(elements.checkoutPaymentMethod?.value || "").trim().toUpperCase() === "VNPAY";
    }

    function moneyRow(label, value, strong) {
        return `
            <div class="checkout-money-row ${strong ? "strong" : ""}">
                <span>${escapeHtml(label)}</span>
                <strong>${formatMoney(value)}</strong>
            </div>
        `;
    }

    function syncCheckoutEmailField() {
        if (!elements.checkoutReceiptEmail) return;

        const enabled = Boolean(elements.checkoutSendEmail?.checked);
        elements.checkoutReceiptEmail.classList.toggle("disabled", !enabled);
    }

    function printCheckoutInvoice(stay, options, existingWindow) {
        const roomLines = stay.roomLines || [];
        const serviceLines = stay.serviceLines || [];
        const rows = roomLines.concat(serviceLines).map(function (line) {
            const name = line.serviceName
                ? `${formatDisplayText(line.serviceName)} x${line.quantity || 1}`
                : `${formatDisplayText(line.roomTypeName)}${line.roomNumber ? " - Phòng " + formatDisplayText(line.roomNumber) : ""}`;
            return `
                <tr>
                    <td>${escapeHtml(name)}</td>
                    <td>${formatMoney(line.unitPrice || 0)}</td>
                    <td>${formatMoney(line.total || 0)}</td>
                </tr>
            `;
        }).join("") || `<tr><td colspan="3">Không có dòng hóa đơn.</td></tr>`;

        const paidAmount = options?.result?.paidAmount ?? stay.paidAmount;
        const remainingAmount = options?.result?.remainingAmount ?? stay.remainingAmount;
        const printWindow = existingWindow || window.open("", "_blank", "width=820,height=900");
        if (!printWindow) {
            showAlert("Trình duyệt đang chặn cửa sổ in hóa đơn.", "warning");
            return;
        }

        printWindow.document.write(`
            <!doctype html>
            <html>
            <head>
                <title>Hóa đơn ${escapeHtml(stay.bookingCode)}</title>
                <style>
                    body { margin: 0; background: #f4f7fb; color: #0f172a; font-family: Arial, sans-serif; }
                    .invoice { max-width: 760px; margin: 24px auto; background: #fff; border: 1px solid #dbe4ef; border-radius: 18px; overflow: hidden; }
                    .head { display: flex; justify-content: space-between; gap: 24px; padding: 26px; border-bottom: 1px solid #e5edf6; }
                    h1 { margin: 6px 0 0; font-size: 28px; }
                    .brand { font-weight: 800; font-size: 18px; }
                    .muted { color: #64748b; }
                    .body { padding: 24px 26px; }
                    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px 24px; margin-bottom: 20px; }
                    .item span { display: block; color: #64748b; font-size: 13px; }
                    .item strong { font-size: 15px; }
                    table { width: 100%; border-collapse: collapse; margin-top: 10px; }
                    th, td { padding: 11px 10px; border-bottom: 1px solid #edf2f7; text-align: left; }
                    th:nth-child(2), th:nth-child(3), td:nth-child(2), td:nth-child(3) { text-align: right; }
                    .total { margin-top: 18px; margin-left: auto; width: 320px; }
                    .total div { display: flex; justify-content: space-between; padding: 8px 0; }
                    .total .strong { font-weight: 800; font-size: 18px; border-top: 1px solid #e2e8f0; margin-top: 6px; padding-top: 12px; }
                    @media print { body { background: #fff; } .invoice { margin: 0; border: 0; border-radius: 0; } }
                </style>
            </head>
            <body>
                <main class="invoice">
                    <section class="head">
                        <div>
                            <div class="brand">Venus Hotel</div>
                            <h1>Hóa đơn trả phòng</h1>
                            <div class="muted">${escapeHtml(new Date().toLocaleString("vi-VN"))}</div>
                        </div>
                        <div style="text-align:right">
                            <div class="muted">Mã đặt phòng</div>
                            <strong>${escapeHtml(stay.bookingCode)}</strong>
                        </div>
                    </section>
                    <section class="body">
                        <div class="grid">
                            <div class="item"><span>Khách hàng</span><strong>${escapeHtml(formatPersonName(stay.customerName))}</strong></div>
                            <div class="item"><span>Số điện thoại</span><strong>${escapeHtml(stay.phoneNumber || "-")}</strong></div>
                            <div class="item"><span>Email</span><strong>${escapeHtml(options?.receiptEmail || stay.email || "-")}</strong></div>
                            <div class="item"><span>Ngày ở</span><strong>${escapeHtml(stay.checkInDate)} - ${escapeHtml(stay.checkOutDate)}</strong></div>
                            <div class="item"><span>Số đêm</span><strong>${stay.nights}</strong></div>
                            <div class="item"><span>Phương thức</span><strong>${escapeHtml(options?.paymentMethod || "-")}</strong></div>
                        </div>
                        <table>
                            <thead><tr><th>Nội dung</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead>
                            <tbody>${rows}</tbody>
                        </table>
                        <section class="total">
                            <div><span>Tiền phòng</span><strong>${formatMoney(stay.roomTotal)}</strong></div>
                            <div><span>Dịch vụ</span><strong>${formatMoney(stay.serviceTotal)}</strong></div>
                            <div><span>Đã thanh toán</span><strong>${formatMoney(paidAmount)}</strong></div>
                            <div><span>Còn lại</span><strong>${formatMoney(remainingAmount)}</strong></div>
                            <div class="strong"><span>Tổng hóa đơn</span><strong>${formatMoney(stay.grandTotal)}</strong></div>
                        </section>
                    </section>
                </main>
                <script>window.addEventListener("load", function () { window.print(); });<\/script>
            </body>
            </html>
        `);
        printWindow.document.close();
    }

    function renderBooking(booking) {
        elements.guestDetails.innerHTML = [
            detailRow("Họ tên", formatPersonName(booking.customerName)),
            detailRow("Số điện thoại", formatSentenceText(booking.phoneNumber || "Chưa có")),
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
            detailRow("Trạng thái booking", formatBookingStatus(booking.bookingStatus)),
            detailRow("Trạng thái hóa đơn", formatInvoiceStatus(booking.invoiceStatus))
        ].join("");

        elements.roomRequirements.innerHTML = booking.requirements.map(function (item) {
            return `
                <div class="requirement-item">
                    <strong>${escapeHtml(formatRoomTypeName(item.roomTypeName))}</strong>
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
                        <strong>${escapeHtml(formatDisplayText(room.roomNumber))}</strong>
                        <small>Tầng ${room.floor}</small>
                        <small>${escapeHtml(formatDisplayText(room.roomTypeName))}</small>
                        <small>${escapeHtml(formatSentenceText(room.statusLabel))}</small>
                    </button>
                `;
            }).join("");

            return `
                <section class="room-type-section" data-room-type-section="${escapeHtml(group.roomTypeId)}" data-required="${group.requiredRooms}">
                    <div class="room-type-header">
                        <strong>${escapeHtml(formatDisplayText(group.roomTypeName))}</strong>
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
            confirmRow("Khách hàng", formatPersonName(state.booking.customerName)),
            confirmRow("Số điện thoại", formatSentenceText(state.booking.phoneNumber || "Chưa có")),
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
                        <strong>Phòng ${escapeHtml(formatDisplayText(room.roomNumber))}</strong>
                        <span>${escapeHtml(formatDisplayText(room.roomTypeName))}</span>
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
                    try {
                        await stopScanner();
                    } catch {
                        state.scannerRunning = false;
                    }

                    if (state.currentStep !== "scan") {
                        return;
                    }

                    elements.bookingCodeInput.value = decodedText;
                    lookupBooking(decodedText);
                });
            state.scannerRunning = true;
            if (state.currentStep !== "scan") {
                await stopScanner();
                return;
            }

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
                        <strong>Phòng ${escapeHtml(formatDisplayText(room.roomNumber))}</strong>
                        <span>${escapeHtml(formatDisplayText(room.roomTypeName))} - Tầng ${room.floor}</span>
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

    function bindCurrencyInput(input, onInput) {
        if (!input) return;

        input.addEventListener("input", function () {
            const amount = getCurrencyInputValue(input);
            input.value = amount > 0 ? formatCurrencyInputValue(amount) : "";
            onInput?.();
        });
        input.addEventListener("blur", function () {
            setCurrencyInputValue(input, getCurrencyInputValue(input));
            onInput?.();
        });
        setCurrencyInputValue(input, getCurrencyInputValue(input));
    }

    function getCurrencyInputValue(input) {
        if (!input) return 0;

        const digits = String(input.value || "").replace(/[^\d]/g, "");
        return digits ? Number(digits) : 0;
    }

    function setCurrencyInputValue(input, value) {
        if (!input) return;

        input.value = formatCurrencyInputValue(value);
    }

    function formatCurrencyInputValue(value) {
        const amount = Math.max(Math.trunc(Number(value) || 0), 0);
        return new Intl.NumberFormat("vi-VN", {
            maximumFractionDigits: 0
        }).format(amount);
    }

    function formatInputDate(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");
        return `${year}-${month}-${day}`;
    }

    function ensureWalkInDateOrder() {
        const checkinDate = elements.walkInCheckinDate?.value || "";
        if (!checkinDate || !elements.walkInCheckoutDate) return;

        if (!elements.walkInCheckoutDate.value || elements.walkInCheckoutDate.value <= checkinDate) {
            const nextDate = new Date(`${checkinDate}T00:00:00`);
            nextDate.setDate(nextDate.getDate() + 1);
            elements.walkInCheckoutDate.value = formatInputDate(nextDate);
        }
    }

    function formatDateLabel(value) {
        if (!value) return "";

        const parts = String(value).split("-");
        if (parts.length !== 3) {
            return value;
        }

        return `${parts[2]}/${parts[1]}/${parts[0]}`;
    }

    function getTodayDateKey() {
        return formatInputDate(new Date());
    }

    function normalizeDateKey(value) {
        const text = String(value || "").trim();
        if (!text) return "";

        const isoMatch = text.match(/^(\d{4})-(\d{1,2})-(\d{1,2})/);
        if (isoMatch) {
            return `${isoMatch[1]}-${isoMatch[2].padStart(2, "0")}-${isoMatch[3].padStart(2, "0")}`;
        }

        const displayMatch = text.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
        if (displayMatch) {
            return `${displayMatch[3]}-${displayMatch[2].padStart(2, "0")}-${displayMatch[1].padStart(2, "0")}`;
        }

        return text;
    }

    function formatBookingStatus(value) {
        const labels = {
            GIU_CHO: "Giữ chỗ",
            DA_DAT_COC: "Đã đặt cọc",
            DA_NHAN_PHONG: "Đã nhận phòng",
            TRA_PHONG: "Đã trả phòng",
            DA_HUY: "Đã hủy",
            QUA_HAN_NHAN_PHONG: "Quá hạn nhận phòng"
        };

        return labels[normalizeCode(value)] || formatCodeLabel(value);
    }

    function formatInvoiceStatus(value) {
        const labels = {
            CHUA_THANH_TOAN: "Chưa thanh toán",
            THANH_TOAN_MOT_PHAN: "Thanh toán một phần",
            DA_THANH_TOAN: "Đã thanh toán",
            DA_HUY: "Đã hủy"
        };

        return labels[normalizeCode(value)] || formatCodeLabel(value);
    }

    function formatRoomTypeName(value) {
        const name = formatDisplayText(value || "Phòng");
        return /^phòng\b/i.test(name) ? name : `Phòng ${name}`;
    }

    function normalizeCode(value) {
        return String(value ?? "")
            .trim()
            .replace(/[\s-]+/g, "_")
            .toUpperCase();
    }

    function formatCodeLabel(value) {
        const text = String(value ?? "").trim();
        if (!text) return "Đang cập nhật";

        return formatDisplayText(text.replace(/[_-]+/g, " "));
    }

    function formatSentenceText(value) {
        const text = String(value ?? "").trim();
        if (!shouldNormalizeUppercase(text)) return text;

        const lower = text.toLocaleLowerCase("vi-VN");
        return lower.replace(/^(\P{L}*)(\p{L})/u, function (_, prefix, letter) {
            return `${prefix}${letter.toLocaleUpperCase("vi-VN")}`;
        });
    }

    function formatPersonName(value) {
        const text = String(value ?? "").trim();
        if (!shouldNormalizeUppercase(text)) return text;

        return text.toLocaleLowerCase("vi-VN").replace(/\p{L}[\p{L}'-]*/gu, function (word) {
            return word.charAt(0).toLocaleUpperCase("vi-VN") + word.slice(1);
        });
    }

    function formatDisplayText(value) {
        const text = String(value ?? "").trim();
        if (!shouldNormalizeUppercase(text)) return text;

        return text.toLocaleLowerCase("vi-VN").replace(/\p{L}[\p{L}'-]*/gu, function (word) {
            return word.charAt(0).toLocaleUpperCase("vi-VN") + word.slice(1);
        });
    }

    function shouldNormalizeUppercase(text) {
        return /[\p{L}]/u.test(text) &&
            /[\p{Lu}]/u.test(text) &&
            !/[\p{Ll}]/u.test(text);
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
