/* ============================================================== */
/* CORE ENGINE: BÃI ĐỖ XE & BẢN ĐỒ AN NINH                        */
/* ============================================================== */

let appConfig = window.APP_CONFIG || { isAdmin: false, currentFloor: 'basement_A', prefixCode: 'A', parkedCars: {} };

let dbCounters = JSON.parse(localStorage.getItem('chkIn_dbCounters'));
if (!dbCounters || (dbCounters.vip + dbCounters.guest + dbCounters.emp !== 240)) {
    dbCounters = { vip: 24, guest: 96, emp: 120 };
    localStorage.setItem('chkIn_dbCounters', JSON.stringify(dbCounters));
}

let cameraList = JSON.parse(localStorage.getItem('chkIn_cameraList')) || [];
let deletedSlots = JSON.parse(localStorage.getItem('chkIn_deletedSlots')) || [];
let cameraOrder = JSON.parse(localStorage.getItem('chkIn_cameraOrder')) || {};
let slotOrderA = JSON.parse(localStorage.getItem('chkIn_slotOrder_A')) || [];
let slotOrderB = JSON.parse(localStorage.getItem('chkIn_slotOrder_B')) || [];

let isCamUpdated = false;
cameraList.forEach(c => {
    if (c.floor === 'B1') { c.floor = 'basement_A'; isCamUpdated = true; }
    if (c.floor === 'B2') { c.floor = 'basement_B'; isCamUpdated = true; }
    if (c.floor === 'ground') { c.floor = 'ground'; }
});
if (isCamUpdated) { localStorage.setItem('chkIn_cameraList', JSON.stringify(cameraList)); }

// ==========================================
// 1. FIX LỖI: CHUẨN HÓA MÃ Ô ĐỖ (A-01 -> A-1)
// ==========================================
let parkedCars = {};
function loadAndNormalizeCars() {
    let tempParkedCars = appConfig.parkedCars || {};
    let dynamicParkedCars = JSON.parse(localStorage.getItem('chkIn_parkedCars')) || {};
    let mergedCars = { ...tempParkedCars, ...dynamicParkedCars };

    parkedCars = {};
    for (let key in mergedCars) {
        // Thuật toán gọt số 0: Ép chuỗi "A-01" thành "A-1" để Bản đồ hiểu được
        let s = key.replace(/\s+/g, '').toUpperCase();
        let parts = s.split('-');
        if (parts.length === 2 && !isNaN(parts[1])) {
            parkedCars[parts[0] + '-' + parseInt(parts[1], 10)] = mergedCars[key];
        } else {
            parkedCars[key] = mergedCars[key];
        }
    }
}
loadAndNormalizeCars(); // Chạy ngay khi nạp file

let totalViolations = 0;
let sortableCameraGrid1 = null;
let sortableParkingGrids = [];

// ==========================================
// THUẬT TOÁN ĐỒNG BỘ TOÀN CẦU (BẢN ĐỒ & CAMERA)
// ==========================================
window.updateGlobalCounters = function () {
    let totalAllSlots = dbCounters.vip + dbCounters.guest + dbCounters.emp - deletedSlots.length;
    let currentOccupied = Object.keys(parkedCars).length;
    let emptySlots = totalAllSlots - currentOccupied;

    // Cập nhật giao diện Bản Đồ & Quản Lý Bãi Đỗ Xe
    const elTotal = document.getElementById('ui-total-slots'); if (elTotal) elTotal.innerText = totalAllSlots;
    const elEmpty = document.getElementById('ui-empty-slots'); if (elEmpty) elEmpty.innerText = emptySlots;
    const elOccupied = document.getElementById('occupiedCount'); if (elOccupied) elOccupied.innerText = currentOccupied;
    const elVio = document.getElementById('violationCount'); if (elVio) elVio.innerText = totalViolations;
    const elProg = document.getElementById('ui-progress-bar'); if (elProg) elProg.style.width = ((emptySlots / totalAllSlots) * 100) + '%';

    // Cập nhật giao diện Trang Camera (Nếu 2 tab đang mở cạnh nhau)
    const camEmpty = document.getElementById('resParkingCount'); if (camEmpty) camEmpty.innerText = emptySlots;
    const camBar = document.getElementById('parkingBar'); if (camBar) camBar.style.width = ((emptySlots / totalAllSlots) * 100) + '%';

    // GỬI TÍN HIỆU ÉP CÁC TAB KHÁC PHẢI ĐỒNG BỘ THEO!
    localStorage.setItem('chkIn_syncEmptySlots', emptySlots);
};

// Lắng nghe tín hiệu đồng bộ
window.addEventListener('storage', function (e) {
    if (e.key === 'chkIn_parkedCars') {
        loadAndNormalizeCars();
        if (typeof renderMainMap === 'function') renderMainMap();
        updateGlobalCounters();
    }
    if (e.key === 'chkIn_motoCount') {
        if (typeof renderMotoMap === 'function') renderMotoMap();
    }
    // NẾU TRANG CAMERA NHẬN ĐƯỢC TÍN HIỆU TỪ BẢN ĐỒ -> TỰ ĐỘNG CHỈNH SỐ!
    if (e.key === 'chkIn_syncEmptySlots') {
        const camEmpty = document.getElementById('resParkingCount');
        if (camEmpty) camEmpty.innerText = e.newValue;
        const camBar = document.getElementById('parkingBar');
        if (camBar) camBar.style.width = ((e.newValue / 240) * 100) + '%';
    }
});

// Tự động đồng bộ Camera ngay khi tải trang
document.addEventListener("DOMContentLoaded", () => {
    const camEmpty = document.getElementById('resParkingCount');
    if (camEmpty && window.location.href.includes('Monitoring')) {
        let syncedEmpty = localStorage.getItem('chkIn_syncEmptySlots');
        if (syncedEmpty) camEmpty.innerText = syncedEmpty;
    }
});

function saveGlobalData() {
    localStorage.setItem('chkIn_dbCounters', JSON.stringify(dbCounters));
    localStorage.setItem('chkIn_cameraList', JSON.stringify(cameraList));
    localStorage.setItem('chkIn_deletedSlots', JSON.stringify(deletedSlots));
    localStorage.setItem('chkIn_slotOrder_A', JSON.stringify(slotOrderA));
    localStorage.setItem('chkIn_slotOrder_B', JSON.stringify(slotOrderB));
    localStorage.setItem('chkIn_cameraOrder', JSON.stringify(cameraOrder));
}

function syncDefaultCameras() {
    let isChanged = false;
    const mainMarkers = document.querySelectorAll('.cam-marker');
    mainMarkers.forEach((marker, index) => {
        const labelEl = marker.querySelector('.cam-label');
        if (labelEl) {
            const name = labelEl.innerText;
            const id = `map-cam-main-${index}`;
            const mapContainer = marker.closest('.blueprint-container') || marker.closest('.basement-clean-layout');
            let floorName = mapContainer ? mapContainer.id.replace('map-', '') : 'ground';

            if (!cameraList.some(c => c.id === id)) {
                cameraList.push({ id: id, name: name, floor: floorName });
                isChanged = true;
            }
        }
    });

    for (let i = 1; i <= (dbCounters.vip + dbCounters.guest); i++) {
        let id = `A-${i}`;
        if (!deletedSlots.includes(id) && !cameraList.some(c => c.id === id)) {
            cameraList.push({ id: id, name: `Cam Ô ${id}`, floor: 'basement_A' }); isChanged = true;
        }
    }
    for (let i = 1; i <= dbCounters.emp; i++) {
        let id = `B-${i}`;
        if (!deletedSlots.includes(id) && !cameraList.some(c => c.id === id)) {
            cameraList.push({ id: id, name: `Cam Ô ${id}`, floor: 'basement_B' }); isChanged = true;
        }
    }
    if (isChanged) saveGlobalData();
}

window.addEventListener('storage', function (e) {
    if (e.key === 'chkIn_parkedCars') {
        loadAndNormalizeCars(); // Reload data an toàn
        if (typeof renderMainMap === 'function') renderMainMap();
        updateGlobalCounters(); // Đẩy số qua tất cả các Tab
    }
    if (e.key === 'chkIn_motoCount') {
        if (typeof renderMotoMap === 'function') renderMotoMap();
    }
});

function removeAccents(str) {
    return str.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
}

// Hàm mới: Ghi chép lịch sử (Vào/Ra) vào bộ nhớ tạm
function addLiveHistory(plate, name, role, action, slot) {
    let history = JSON.parse(localStorage.getItem('chkIn_liveHistory')) || [];
    history.unshift({
        time: new Date().toLocaleTimeString('vi-VN', { hour12: false }),
        plate: plate, name: name, role: role, action: action, slot: slot
    });
    if (history.length > 30) history.pop(); // Chỉ giữ 30 lượt gần nhất cho nhẹ web
    localStorage.setItem('chkIn_liveHistory', JSON.stringify(history));
    // Phát tín hiệu cho trang Bãi Đỗ Xe cập nhật bảng lịch sử ngay lập tức
    localStorage.setItem('chkIn_triggerUpdateTable', Date.now());
}

window.processCameraScan = function (vehicleType, userRole, plateNumber, ownerName) {
    // ==========================================
    // 1. LOGIC CHECK-OUT: XE ĐI RA
    // ==========================================
    let existingSlot = null;
    for (let slot in parkedCars) {
        if (parkedCars[slot].plate === plateNumber) {
            existingSlot = slot;
            break;
        }
    }

    if (existingSlot) {
        let role = parkedCars[existingSlot].role; // Lấy role cũ để ghi log
        delete parkedCars[existingSlot];

        let currentDynamic = JSON.parse(localStorage.getItem('chkIn_parkedCars')) || {};
        delete currentDynamic[existingSlot];
        localStorage.setItem('chkIn_parkedCars', JSON.stringify(currentDynamic));

        // Ghi vào nhật ký Live
        addLiveHistory(plateNumber, ownerName, role, 'OUT', existingSlot);

        if (typeof renderMainMap === 'function') renderMainMap();
        updateGlobalCounters();

        Swal.fire({
            icon: 'info', title: 'XÁC NHẬN ĐI RA',
            html: `Xe: <b>${plateNumber}</b><br>Chủ xe: <b>${ownerName}</b><br>Đã trả lại ô đỗ: <b class="text-danger fs-4">${existingSlot}</b>`,
            timer: 3000
        });
        return "OUT";
    }

    // ==========================================
    // 2. LOGIC CHECK-IN: XE ĐI VÀO 
    // ==========================================
    if (vehicleType === 'Oto') {
        let safeRole = removeAccents(String(userRole || '')).toLowerCase();
        let uiText = document.body.innerText.toLowerCase();
        let safeUiText = removeAccents(uiText);

        let targetPrefix = 'A';
        let standardizedRole = 'Khách';

        // Phân biệt chính xác Nhân viên / VIP / Khách
        if (safeRole.includes('nhan vien') || safeRole.includes('nv') || safeRole.includes('phong') || safeRole.includes('ke toan') || safeRole.includes('it') ||
            safeUiText.includes('phong ke toan') || safeUiText.includes('phong nhan su') || safeUiText.includes('phong cong nghe')) {
            targetPrefix = 'B'; standardizedRole = 'Nhân Viên';
        }
        else if (safeRole.includes('sep') || safeRole.includes('vip') || safeRole.includes('giam doc') || safeRole.includes('bod') ||
            safeUiText.includes('ban giam doc') || safeUiText.includes('bod')) {
            targetPrefix = 'A'; standardizedRole = 'VIP';
        }

        let assignedSlot = findEmptySlot(targetPrefix, standardizedRole, parkedCars);

        if (assignedSlot) {
            parkedCars[assignedSlot] = { role: standardizedRole, plate: plateNumber, name: ownerName, time: new Date().toLocaleTimeString() };

            let currentDynamic = JSON.parse(localStorage.getItem('chkIn_parkedCars')) || {};
            currentDynamic[assignedSlot] = parkedCars[assignedSlot];
            localStorage.setItem('chkIn_parkedCars', JSON.stringify(currentDynamic));

            // Ghi vào nhật ký Live
            addLiveHistory(plateNumber, ownerName, standardizedRole, 'IN', assignedSlot);

            if (typeof renderMainMap === 'function') renderMainMap();
            updateGlobalCounters();

            Swal.fire({
                icon: 'success', title: 'NHẬN DIỆN VÀO BÃI',
                html: `Xe: <b>${plateNumber}</b><br>Chủ xe: <b>${ownerName}</b> (${standardizedRole})<br>Chỉ định ô đỗ: <b class="text-primary fs-4">${assignedSlot}</b>`,
                timer: 3000
            });
            return assignedSlot;
        } else {
            Swal.fire('Hết chỗ!', `Khu vực Hầm đã kín chỗ!`, 'error');
            return null;
        }
    }
}

function findEmptySlot(prefix, role, currentData) {
    let startIdx = 1;
    let maxSlots = prefix === 'A' ? (dbCounters.vip + dbCounters.guest) : dbCounters.emp;

    if (prefix === 'A') {
        if (role === 'VIP') { maxSlots = dbCounters.vip; }
        else { startIdx = dbCounters.vip + 1; }
    }

    for (let i = startIdx; i <= maxSlots; i++) {
        let slotId = `${prefix}-${i}`;
        if (!currentData[slotId] && !deletedSlots.includes(slotId)) return slotId;
    }
    return null;
}

function buildExpectedSlotOrder(prefix, maxCount, currentOrder) {
    let expectedSlots = [];
    for (let i = 1; i <= maxCount; i++) {
        let id = `${prefix}-${i}`;
        if (!deletedSlots.includes(id)) expectedSlots.push(id);
    }
    let newOrder = currentOrder.filter(id => expectedSlots.includes(id));
    let missing = expectedSlots.filter(id => !newOrder.includes(id));
    return newOrder.concat(missing);
}

window.renderMainMap = function () {
    totalViolations = 0;
    const containerA = document.getElementById('dynamic-parking-A');
    const containerB = document.getElementById('dynamic-parking-B');
    const containerSingle = document.getElementById('dynamic-main-parking-grid');

    if (containerA) renderGridForFloor('A', containerA);
    if (containerB) renderGridForFloor('B', containerB);
    if (containerSingle) renderGridForFloor(appConfig.prefixCode, containerSingle);

    updateGlobalCounters();
    if (typeof renderMotoMap === 'function') renderMotoMap();

    if (appConfig.isAdmin) {
        document.querySelectorAll('.parking-grid').forEach(grid => {
            sortableParkingGrids.push(new Sortable(grid, {
                group: 'shared-parking', animation: 150, ghostClass: 'sortable-ghost',
                onEnd: function (evt) {
                    let blockContainer = evt.to.closest('.basement-clean-layout') || evt.to.closest('.col-12');
                    if (!blockContainer) return;
                    let newOrder = [];
                    blockContainer.querySelectorAll('.parking-slot').forEach(el => newOrder.push(el.getAttribute('data-id')));

                    if (blockContainer.id === 'map-basement_A' || appConfig.prefixCode === 'A') {
                        slotOrderA = newOrder; localStorage.setItem('chkIn_slotOrder_A', JSON.stringify(slotOrderA));
                    } else {
                        slotOrderB = newOrder; localStorage.setItem('chkIn_slotOrder_B', JSON.stringify(slotOrderB));
                    }
                }
            }));
        });
    }
}

function renderGridForFloor(prefix, container) {
    let totalSlotsInFloor = prefix === 'A' ? (dbCounters.vip + dbCounters.guest) : dbCounters.emp;
    let targetOrder = prefix === 'A' ? slotOrderA : slotOrderB;
    targetOrder = buildExpectedSlotOrder(prefix, totalSlotsInFloor, targetOrder);

    let html = '';
    const slotsPerBlock = 24;

    for (let i = 0; i < targetOrder.length; i += slotsPerBlock) {
        let chunk = targetOrder.slice(i, i + slotsPerBlock);
        html += `<div class="parking-block mb-3"><div class="parking-grid">`;

        chunk.forEach(slotId => {
            let slotNum = parseInt(slotId.split('-')[1]);
            let parkedCarInfo = parkedCars[slotId];
            let isOccupied = !!parkedCarInfo;
            let isViolation = false;

            let roleClass = ''; let roleLabel = ''; let roleIcon = ''; let intendedRole = '';

            if (prefix === 'A') {
                if (slotNum <= dbCounters.vip) { intendedRole = 'VIP'; roleClass = 'slot-vip'; roleLabel = 'VIP/SẾP'; roleIcon = '<i class="bi bi-star-fill mb-1"></i>'; }
                else { intendedRole = 'Khách'; roleClass = 'slot-customer'; roleLabel = 'KHÁCH'; roleIcon = '<i class="bi bi-person-badge mb-1"></i>'; }
            } else {
                intendedRole = 'Nhân Viên'; roleClass = 'slot-employee'; roleLabel = 'NHÂN VIÊN'; roleIcon = '<i class="bi bi-person-vcard mb-1"></i>';
            }

            if (isOccupied) {
                let actualSafe = removeAccents(parkedCarInfo.role);
                let intendedSafe = removeAccents(intendedRole);

                if (intendedSafe === 'vip' && !actualSafe.includes('vip') && !actualSafe.includes('sep') && !actualSafe.includes('giam doc')) {
                    isViolation = true; totalViolations++;
                }
                if (intendedSafe === 'khach' && (actualSafe.includes('nhan vien') || actualSafe.includes('nv'))) {
                    isViolation = true; totalViolations++;
                }
                if (intendedSafe === 'nhan vien' && (actualSafe.includes('khach') || actualSafe.includes('vip') || actualSafe.includes('sep'))) {
                    isViolation = true; totalViolations++;
                }
            }

            let cssClass = isOccupied ? (isViolation ? 'slot-violation' : 'slot-occupied') : roleClass;
            let content = '';
            if (isOccupied) {
                if (isViolation) {
                    content = `<i class="bi bi-exclamation-triangle-fill text-danger fs-3 mb-1"></i><span style="font-size:0.55rem; color:#fff" class="bg-danger px-1 rounded mt-1">${parkedCarInfo.plate}</span>`;
                } else {
                    content = `<i class="bi bi-car-front-fill car-icon animate__animated animate__zoomIn text-success"></i><span style="font-size:0.55rem; color:#fff" class="bg-success px-1 rounded mt-1">${parkedCarInfo.plate}</span>`;
                }
            } else {
                content = `${roleIcon}<span style="font-size: 0.5rem; opacity: 0.8">${roleLabel}</span><span class="fw-bold mt-1" style="font-size: 0.8rem;">${slotId}</span>`;
            }

            let adminDeleteBtn = appConfig.isAdmin ? `<button class="btn btn-danger position-absolute top-0 end-0 p-0 rounded-circle d-flex align-items-center justify-content-center btn-delete-slot" onclick="deleteSlot('${slotId}', event)"><i class="bi bi-x" style="font-size: 14px; margin: 0;"></i></button>` : '';
            let camIcon = `<i class="bi bi-webcam-fill slot-cam text-info" onclick="openSlotCamera(event, '${slotId}')"></i>`;

            html += `<div class="parking-slot ${cssClass}" data-id="${slotId}" onclick="showSlotInfo('${slotId}', '${isOccupied ? parkedCarInfo.name : ''}', '${isOccupied ? parkedCarInfo.plate : ''}', '')">${adminDeleteBtn}${camIcon}${content}</div>`;
        });
        html += `</div></div>`;

        if (i + slotsPerBlock < targetOrder.length) {
            html += `<div class="driveway-clean"><i class="bi bi-chevron-double-left fs-3 driveway-arrow left"></i><span><i class="bi bi-car-front"></i> LÀN LƯU THÔNG CHÍNH <i class="bi bi-car-front"></i></span><i class="bi bi-chevron-double-right fs-3 driveway-arrow right"></i></div>`;
        }
    }
    container.innerHTML = html;
}

window.renderMotoMap = function () {
    let motoCount = parseInt(localStorage.getItem('chkIn_motoCount')) || 0;
    let uiMoto = document.getElementById('ui-moto-count');
    if (uiMoto) uiMoto.innerText = motoCount;

    let motoGrid = document.getElementById('moto-parking-grid');
    if (motoGrid) {
        motoGrid.innerHTML = '';
        for (let i = 0; i < motoCount; i++) {
            motoGrid.innerHTML += `<i class="bi bi-bicycle text-success animate__animated animate__zoomIn m-2" style="font-size: 3.5rem; filter: drop-shadow(0 0 5px rgba(25, 135, 84, 0.8));"></i>`;
        }
    }
}

window.changeCamLayout = function (type) {
    const grid = document.getElementById('main-camera-grid') || document.getElementById('cctv-grid-view');
    if (!grid) return;
    grid.className = 'cctv-layout-wrapper gap-3 cctv-layout-' + type;

    ['grid', 'focus', 'list', 'free'].forEach(l => {
        const btn = document.getElementById('btn-layout-' + l);
        if (btn) {
            if (l === type) { btn.classList.add('active', 'btn-primary'); btn.classList.remove('btn-dark', 'border-secondary'); }
            else { btn.classList.remove('active', 'btn-primary'); btn.classList.add('btn-dark', 'border-secondary'); }
        }
    });
    localStorage.setItem('chkIn_camLayoutPref', type);
}

window.renderCameras = function () {
    syncDefaultCameras();
    const currentFloorName = appConfig.currentFloor;
    const grid = document.getElementById('main-camera-grid') || document.getElementById('cctv-grid-view');
    if (!grid) return;
    grid.innerHTML = '';

    let currentCamOrder = cameraOrder[currentFloorName] || [];
    let camsInFloor = cameraList.filter(c => c.floor === currentFloorName);

    let orderedCams = currentCamOrder.map(id => camsInFloor.find(c => c.id === id)).filter(c => c);
    let missingCams = camsInFloor.filter(c => !currentCamOrder.includes(c.id));
    let finalRenderList = orderedCams.concat(missingCams);

    finalRenderList.forEach(cam => {
        const isMain = cam.id.includes('MAIN') || cam.id.includes('map-cam-main');
        const fallbackWidth = isMain ? 'width: 450px; height: 280px;' : 'width: 320px; height: 200px;';

        const html = `
            <div class="dynamic-main-cam" data-id="${cam.id}">
                <div class="cam-feed rounded-2" style="${fallbackWidth}">
                    <div class="cam-content"><i class="bi bi-camera-video text-white opacity-25"></i></div>
                    <div class="cam-status rounded-1"><i class="bi bi-circle-fill text-white small me-1"></i> ONLINE</div>
                    <div class="cam-title"><i class="bi bi-geo-alt-fill text-danger me-1"></i><span class="cam-name-text">${cam.name}</span></div>
                    <button class="btn-fullscreen rounded-1" onclick="toggleFullscreen(this.parentElement)" title="Phóng to"><i class="bi bi-arrows-fullscreen"></i></button>
                    <div class="cam-overlay"><span>CHK-IN // ${cam.id}</span><span class="live-clock-cam text-info fw-bold"></span></div>
                </div>
            </div>`;
        grid.insertAdjacentHTML('beforeend', html);
    });

    const savedLayout = localStorage.getItem('chkIn_camLayoutPref') || 'grid';
    changeCamLayout(savedLayout);

    if (appConfig.isAdmin) {
        if (sortableCameraGrid1) sortableCameraGrid1.destroy();
        sortableCameraGrid1 = new Sortable(grid, {
            animation: 150, ghostClass: 'sortable-ghost', handle: '.cam-feed',
            onEnd: function (evt) {
                let newCamOrder = Array.from(grid.querySelectorAll('.dynamic-main-cam')).map(el => el.getAttribute('data-id'));
                cameraOrder[currentFloorName] = newCamOrder;
                localStorage.setItem('chkIn_cameraOrder', JSON.stringify(cameraOrder));
            }
        });
    }
}

// ZOOM BẢN ĐỒ
let scale = 1; let panning = false; let pointX = 0; let pointY = 0; let startX, startY;
const frame = document.getElementById('blueprint-frame');
const zoomWrapper = document.getElementById('map-zoom-wrapper');
if (frame && zoomWrapper) {
    function setTransform() { zoomWrapper.style.transform = `translate(${pointX}px, ${pointY}px) scale(${scale})`; }
    frame.addEventListener('mousedown', (e) => { if (e.target.closest('button') || e.target.closest('.cam-marker') || e.target.closest('.slot-cam') || e.target.closest('.parking-slot')) return; e.preventDefault(); panning = true; frame.style.cursor = 'grabbing'; startX = e.clientX - pointX; startY = e.clientY - pointY; });
    frame.addEventListener('mousemove', (e) => { if (!panning) return; pointX = e.clientX - startX; pointY = e.clientY - startY; setTransform(); });
    frame.addEventListener('mouseup', () => { panning = false; frame.style.cursor = 'grab'; });
    frame.addEventListener('mouseleave', () => { panning = false; frame.style.cursor = 'grab'; });
    frame.addEventListener('wheel', (e) => {
        e.preventDefault();
        const xs = (e.clientX - frame.getBoundingClientRect().left - pointX) / scale; const ys = (e.clientY - frame.getBoundingClientRect().top - pointY) / scale;
        const delta = (e.wheelDelta ? e.wheelDelta : -e.deltaY); (delta > 0) ? (scale *= 1.1) : (scale /= 1.1);
        scale = Math.max(0.5, Math.min(scale, 3));
        pointX = e.clientX - frame.getBoundingClientRect().left - xs * scale; pointY = e.clientY - frame.getBoundingClientRect().top - ys * scale;
        setTransform();
    });
    window.zoomMap = function (amount) { scale += amount; scale = Math.max(0.5, Math.min(scale, 3)); setTransform(); }
    window.resetZoom = function () { scale = 1; pointX = 0; pointY = 0; setTransform(); }
}

window.switchTab = function (tab) {
    const btnB = document.getElementById('btn-tab-blueprint') || document.getElementById('tab-sodo');
    const btnC = document.getElementById('btn-tab-camera') || document.getElementById('tab-cctv');
    const viewB = document.getElementById('view-blueprint') || document.getElementById('content-sodo');
    const viewC = document.getElementById('view-camera') || document.getElementById('content-cctv');

    if (tab === 'blueprint' || tab === 'sodo') {
        if (btnB) { btnB.classList.add('active', 'btn-primary'); btnB.classList.remove('btn-light', 'text-muted', 'border-0'); }
        if (btnC) { btnC.classList.remove('active', 'btn-primary'); btnC.classList.add('btn-light', 'text-muted', 'border-0'); }
        if (viewB) { viewB.classList.remove('d-none'); viewB.classList.add('show', 'active'); }
        if (viewC) { viewC.classList.add('d-none'); viewC.classList.remove('show', 'active'); }
    } else {
        if (btnC) { btnC.classList.add('active', 'btn-primary'); btnC.classList.remove('btn-light', 'text-muted', 'border-0'); }
        if (btnB) { btnB.classList.remove('active', 'btn-primary'); btnB.classList.add('btn-light', 'text-muted', 'border-0'); }
        if (viewC) { viewC.classList.remove('d-none'); viewC.classList.add('show', 'active'); }
        if (viewB) { viewB.classList.add('d-none'); viewB.classList.remove('show', 'active'); }
        renderCameras();
    }
}

window.openSlotCamera = function (event, slotId) {
    event.stopPropagation(); switchTab('camera');
    setTimeout(() => {
        let targetCam = document.querySelector(`.dynamic-main-cam[data-id="${slotId}"]`);
        if (targetCam) {
            targetCam.scrollIntoView({ behavior: 'smooth', block: 'center' });
            targetCam.firstElementChild.style.borderColor = '#ffc107'; targetCam.firstElementChild.style.boxShadow = '0 0 20px rgba(255,193,7,0.8)';
            setTimeout(() => { targetCam.firstElementChild.style.borderColor = '#333'; targetCam.firstElementChild.style.boxShadow = '0 4px 10px rgba(0,0,0,0.5)'; }, 2500);
        }
    }, 100);
}

window.toggleFullscreen = function (camElement) {
    if (!document.fullscreenElement) {
        camElement.classList.add('cam-fullscreen'); camElement.querySelector('.btn-fullscreen').innerHTML = '<i class="bi bi-fullscreen-exit text-danger fs-3"></i>';
        if (camElement.requestFullscreen) { camElement.requestFullscreen(); } else if (camElement.webkitRequestFullscreen) { camElement.webkitRequestFullscreen(); }
    } else {
        if (document.exitFullscreen) { document.exitFullscreen(); }
    }
}
document.addEventListener('fullscreenchange', () => {
    if (!document.fullscreenElement) document.querySelectorAll('.cam-fullscreen').forEach(el => { el.classList.remove('cam-fullscreen'); el.querySelector('.btn-fullscreen').innerHTML = '<i class="bi bi-arrows-fullscreen"></i>'; });
});

setInterval(() => {
    const now = new Date(); const timeStr = now.toLocaleTimeString('en-US', { hour12: false }) + ':' + now.getMilliseconds().toString().padStart(3, '0').substring(0, 2);
    document.querySelectorAll('.live-clock-cam').forEach(el => el.innerText = timeStr);
}, 50);

document.addEventListener("DOMContentLoaded", () => {
    syncDefaultCameras();
    if (window.switchFloor) { switchFloor(); }
    else { renderMainMap(); }
    updateGlobalCounters(); // Đảm bảo số liệu đúng khi vừa vào trang
});

window.deleteSlot = function (slotId, event) {
    event.stopPropagation();
    Swal.fire({
        title: 'Xóa vị trí?', text: `Gỡ bỏ ô đỗ ${slotId}? Việc này sẽ làm giảm tổng số lượng bãi.`, icon: 'warning',
        showCancelButton: true, confirmButtonColor: '#d33', cancelButtonText: 'Hủy', confirmButtonText: 'Xóa ngay'
    }).then((result) => {
        if (result.isConfirmed) {
            if (slotId.startsWith('A')) { dbCounters.guest--; slotOrderA = slotOrderA.filter(id => id !== slotId); localStorage.setItem('chkIn_slotOrder_A', JSON.stringify(slotOrderA)); }
            else { dbCounters.emp--; slotOrderB = slotOrderB.filter(id => id !== slotId); localStorage.setItem('chkIn_slotOrder_B', JSON.stringify(slotOrderB)); }

            deletedSlots.push(slotId);
            cameraList = cameraList.filter(c => c.id !== slotId);
            saveGlobalData(); renderMainMap();
        }
    });
}

window.showSlotInfo = function (id, name, plate, img) {
    let card = document.getElementById('slotDetailCard');
    if (!card) return;
    card.classList.remove('d-none'); document.getElementById('lblSlotId').innerText = id;
    if (name && name !== 'null' && name !== '') { document.getElementById('slotUser').innerText = name; document.getElementById('slotPlate').innerText = plate; document.getElementById('slotImg').src = img || '/images/no-avatar.png'; }
    else { window.resetSlot(); Swal.fire({ title: 'Vị trí ' + id, text: 'Vị trí này hiện đang trống.', icon: 'info', timer: 1500, showConfirmButton: false }); }
}
window.resetSlot = function () { let card = document.getElementById('slotDetailCard'); if (card) card.classList.add('d-none'); }