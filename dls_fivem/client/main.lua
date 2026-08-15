local localVehicleState = {}
local localSirenTone = 0
local localTaMode = 0

local function getDriverVehicle()
    local ped = PlayerPedId()
    if not IsPedInAnyVehicle(ped, false) then
        return nil
    end

    local vehicle = GetVehiclePedIsIn(ped, false)
    if vehicle == 0 or GetPedInVehicleSeat(vehicle, -1) ~= ped then
        return nil
    end

    return vehicle
end

local function getIndicatorStateName(indicatorState)
    if indicatorState == 0 then return 'off' end
    if indicatorState == 1 then return 'left' end
    if indicatorState == 2 then return 'right' end
    if indicatorState == 3 then return 'both' end
    return 'off'
end

local function submitState(vehicle)
    local netId = NetworkGetNetworkIdFromEntity(vehicle)
    if netId == 0 then
        return
    end

    local state = localVehicleState[vehicle] or {
        stage = 0,
        sirenTone = localSirenTone,
        taMode = localTaMode,
        extras = {},
        indicators = getIndicatorStateName(GetVehicleIndicatorLights(vehicle))
    }

    TriggerServerEvent('dlsv2_fivem:setVehicleState', netId, state)
end

local function ensureLocalState(vehicle)
    if not localVehicleState[vehicle] then
        localVehicleState[vehicle] = {
            stage = 0,
            sirenTone = 0,
            taMode = 0,
            extras = {},
            indicators = 'off'
        }
    end

    return localVehicleState[vehicle]
end

local function applyState(vehicle, state)
    if not DoesEntityExist(vehicle) or type(state) ~= 'table' then
        return
    end

    local stage = tonumber(state.stage) or 0
    SetVehicleSiren(vehicle, stage > 0)
    SetVehicleHasMutedSirens(vehicle, true)

    if type(state.extras) == 'table' then
        for extraId, enabled in pairs(state.extras) do
            local id = tonumber(extraId)
            if id and DoesExtraExist(vehicle, id) then
                SetVehicleExtra(vehicle, id, enabled and 0 or 1)
            end
        end
    end

    local indicators = tostring(state.indicators or 'off')
    if indicators == 'left' then
        SetVehicleIndicatorLights(vehicle, 0, true)
        SetVehicleIndicatorLights(vehicle, 1, false)
    elseif indicators == 'right' then
        SetVehicleIndicatorLights(vehicle, 0, false)
        SetVehicleIndicatorLights(vehicle, 1, true)
    elseif indicators == 'both' then
        SetVehicleIndicatorLights(vehicle, 0, true)
        SetVehicleIndicatorLights(vehicle, 1, true)
    else
        SetVehicleIndicatorLights(vehicle, 0, false)
        SetVehicleIndicatorLights(vehicle, 1, false)
    end
end

AddStateBagChangeHandler(DLSV2.StateBagKey, nil, function(bagName, _, value)
    local entity = GetEntityFromStateBagName(bagName)
    if entity == 0 or not DoesEntityExist(entity) then
        return
    end

    if GetEntityType(entity) ~= 2 then
        return
    end

    applyState(entity, value)
end)

RegisterCommand('dls_stage_cycle', function()
    local vehicle = getDriverVehicle()
    if not vehicle then return end

    local state = ensureLocalState(vehicle)
    state.stage = (state.stage + 1) % (DLSV2.MaxStage + 1)
    submitState(vehicle)
end, false)
RegisterKeyMapping('dls_stage_cycle', 'DLSv2: Cycle stage', 'keyboard', DLSV2.DefaultControls.stageCycle)

RegisterCommand('dls_stage_toggle', function()
    local vehicle = getDriverVehicle()
    if not vehicle then return end

    local state = ensureLocalState(vehicle)
    if state.stage == 0 then state.stage = 1 else state.stage = 0 end
    submitState(vehicle)
end, false)
RegisterKeyMapping('dls_stage_toggle', 'DLSv2: Toggle stage', 'keyboard', DLSV2.DefaultControls.stageToggle)

RegisterCommand('dls_siren_toggle', function()
    local vehicle = getDriverVehicle()
    if not vehicle then return end

    local state = ensureLocalState(vehicle)
    if state.sirenTone == 0 then
        state.sirenTone = 1
    else
        state.sirenTone = 0
    end
    submitState(vehicle)
end, false)
RegisterKeyMapping('dls_siren_toggle', 'DLSv2: Toggle siren tone', 'keyboard', DLSV2.DefaultControls.sirenToggle)

RegisterCommand('dls_siren_cycle', function()
    local vehicle = getDriverVehicle()
    if not vehicle then return end

    local state = ensureLocalState(vehicle)
    state.sirenTone = (state.sirenTone + 1) % 4
    submitState(vehicle)
end, false)
RegisterKeyMapping('dls_siren_cycle', 'DLSv2: Cycle siren tone', 'keyboard', DLSV2.DefaultControls.sirenCycle)

RegisterCommand('dls_ta_cycle', function()
    local vehicle = getDriverVehicle()
    if not vehicle then return end

    local state = ensureLocalState(vehicle)
    state.taMode = (state.taMode + 1) % 5
    submitState(vehicle)
end, false)
RegisterKeyMapping('dls_ta_cycle', 'DLSv2: Cycle traffic advisor mode', 'keyboard', DLSV2.DefaultControls.taCycle)
