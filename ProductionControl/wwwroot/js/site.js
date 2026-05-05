// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("DOMContentLoaded", () => {
	const SCROLL_POSITION_KEY = "productionControl.scrollPosition";
	const lineStates = new Map();

	const translateStatus = (status) => {
		switch (status) {
			case "Pending":
				return "Ожидает";
			case "InProgress":
				return "В работе";
			case "Completed":
				return "Завершен";
			case "Cancelled":
				return "Отменен";
			case "Active":
				return "Работает";
			case "Stopped":
				return "Остановлена";
			default:
				return status;
		}
	};

	const clamp = (value, min, max) => Math.min(Math.max(value, min), max);

	const restoreScrollPosition = () => {
		const storedValue = sessionStorage.getItem(SCROLL_POSITION_KEY);
		if (!storedValue) {
			return;
		}

		const scrollY = Number(storedValue);
		if (!Number.isNaN(scrollY)) {
			window.scrollTo({ top: scrollY, behavior: "auto" });
		}

		sessionStorage.removeItem(SCROLL_POSITION_KEY);
	};

	const registerScrollPersistence = () => {
		document.querySelectorAll(".dashboard-grid form").forEach((form) => {
			form.addEventListener("submit", () => {
				sessionStorage.setItem(SCROLL_POSITION_KEY, String(window.scrollY));
			});
		});
	};

	const registerLineStatusProgressCapture = () => {
		document.querySelectorAll(".line-status-form").forEach((form) => {
			form.addEventListener("submit", () => {
				const card = form.closest(".line-card");
				const progressText = card?.querySelector(".line-progress-value");
				const hiddenInput = form.querySelector('input[name="displayedProgress"]');

				if (!progressText || !hiddenInput) {
					return;
				}

				const displayValue = Number(progressText.textContent || 0);
				if (!Number.isNaN(displayValue)) {
					hiddenInput.value = String(Math.max(0, Math.min(100, Math.round(displayValue))));
				}
			});
		});
	};

	const calculateDisplayProgress = (state) => {
		if (!state || !state.orderId) {
			return 0;
		}

		if (state.orderStatus === "Completed") {
			return 100;
		}

		let progress = Number(state.serverProgress || 0);
		const hasTimeline = state.startMs && state.endMs && state.endMs > state.startMs;

		if (state.lineStatus === "Active" && state.orderStatus === "InProgress" && hasTimeline) {
			const now = Date.now();
			const timelineProgress = ((now - state.startMs) / (state.endMs - state.startMs)) * 100;
			progress = Math.max(progress, timelineProgress);

			if (now < state.endMs) {
				progress = Math.min(progress, 99.4);
			}
		}

		return Math.round(clamp(progress, 0, 100));
	};

	const renderLineByState = (lineId) => {
		const state = lineStates.get(lineId);
		if (!state) {
			return;
		}

		const card = document.querySelector(`[data-line-id="${lineId}"]`);
		if (!card) {
			return;
		}

		const statusElement = card.querySelector(".line-status");
		applyStatusClass(statusElement, state.lineStatus);
		if (statusElement) {
			statusElement.textContent = translateStatus(state.lineStatus);
		}

		const productNameElement = card.querySelector(".line-product-name");
		if (productNameElement) {
			productNameElement.textContent = state.productName || "Нет активного заказа";
		}

		const progressValue = calculateDisplayProgress(state);
		const progressFill = card.querySelector(".progress-fill");
		if (progressFill) {
			progressFill.style.width = `${progressValue}%`;
		}

		const progressText = card.querySelector(".line-progress-value");
		if (progressText) {
			progressText.textContent = String(progressValue);
		}
	};

	const renderAllLines = () => {
		lineStates.forEach((_state, lineId) => renderLineByState(lineId));
	};

	const applyStatusClass = (element, status) => {
		if (!element) {
			return;
		}

		element.className = `status-pill ${status ? `status-${status.toLowerCase()}` : ""}`;
	};

	const refreshLiveData = async () => {
		try {
			const [linesResponse, ordersResponse] = await Promise.all([
				fetch("/api/lines", { cache: "no-store" }),
				fetch("/api/orders", { cache: "no-store" })
			]);

			if (!linesResponse.ok || !ordersResponse.ok) {
				return;
			}

			const [lines, orders] = await Promise.all([linesResponse.json(), ordersResponse.json()]);

			lines.forEach((line) => {
				lineStates.set(line.id, {
					lineStatus: line.status,
					orderId: line.currentWorkOrder?.id ?? null,
					orderStatus: line.currentWorkOrder?.status ?? null,
					productName: line.currentWorkOrder?.product?.name ?? null,
					serverProgress: Number(line.currentWorkOrder?.progressPercent || 0),
					startMs: line.currentWorkOrder?.startDate ? Date.parse(line.currentWorkOrder.startDate) : null,
					endMs: line.currentWorkOrder?.estimatedEndDate ? Date.parse(line.currentWorkOrder.estimatedEndDate) : null
				});
			});

			renderAllLines();

			orders.forEach((order) => {
				const row = document.querySelector(`[data-order-id="${order.id}"]`);
				if (!row) {
					return;
				}

				const statusElement = row.querySelector(".order-status");
				applyStatusClass(statusElement, order.status);
				if (statusElement) {
					statusElement.textContent = translateStatus(order.status);
				}

				const deadlineElement = row.querySelector(".order-deadline");
				if (deadlineElement && order.estimatedEndDate) {
					const endDate = new Date(order.estimatedEndDate);
					deadlineElement.textContent = endDate.toLocaleString("ru-RU", {
						day: "2-digit",
						month: "short",
						hour: "2-digit",
						minute: "2-digit"
					});
				}
			});
		} catch {
			// Ignore transient network errors while polling.
		}
	};

	const orderForm = document.querySelector("form[data-order-form]");
	if (orderForm) {
		const productSelect = orderForm.querySelector('select[name="ProductId"]');
		const quantityInput = orderForm.querySelector('input[name="Quantity"]');
		const lineSelect = orderForm.querySelector('select[name="ProductionLineId"]');
		const preview = document.createElement("p");
		preview.className = "helper-text";
		orderForm.appendChild(preview);

		const refreshPreview = () => {
			const productOption = productSelect?.options[productSelect.selectedIndex];
			const lineOption = lineSelect?.options[lineSelect.selectedIndex];
			const productionTime = Number(productOption?.dataset.productionTime || 0);
			const efficiency = Number(lineOption?.dataset.efficiency || 1) || 1;
			const quantity = Number(quantityInput?.value || 0);

			if (!productionTime || !quantity) {
				preview.textContent = "Выберите продукт и количество, чтобы увидеть расчет срока.";
				return;
			}

			const minutes = Math.round((quantity * productionTime) / efficiency);
			preview.textContent = `Расчетное время производства: ${minutes} мин${lineOption?.value ? ` на линии ${lineOption.textContent.trim()}` : ""}.`;
		};

		productSelect?.addEventListener("change", refreshPreview);
		quantityInput?.addEventListener("input", refreshPreview);
		lineSelect?.addEventListener("change", refreshPreview);
		refreshPreview();
	}

	registerScrollPersistence();
	registerLineStatusProgressCapture();
	restoreScrollPosition();

	refreshLiveData();
	setInterval(refreshLiveData, 2000);
	setInterval(renderAllLines, 250);
});
