local function isNumber(n)
    return type(n) == 'number'
end

local function normalizeState(state)
    if type(state) ~= 'table' then
        return nil
    end

    local normalized = {
        stage = math.max(0, math.min(DLSV2.MaxStage, tonumber(state.stage) or 0)),
        sirenTone = math.max(0, math.min(3, tonumber(state.sirenTone) or 0)),
        taMode = math.max(0, math.min(4, tonumber(state.taMode) or 0)),
        extras = {},
        indicators = tostring(state.indicators or 'off')
    }

    if type(state.extras) == 'table' then
        for extraId, enabled in pairs(state.extras) do
            local id = tonumber(extraId)
            if isNumber(id) and id >= 0 and id <= 20 then
                normalized.extras[id] = enabled and true or false
            end
        end
    end

    if normalized.indicators ~= 'off' and normalized.indicators ~= 'left' and normalized.indicators ~= 'right' and normalized.indicators ~= 'both' then
        normalized.indicators = 'off'
    end

    return normalized
end

RegisterNetEvent('dlsv2_fivem:setVehicleState', function(netId, state)
    if type(netId) ~= 'number' then
        return
    end

    local vehicle = NetworkGetEntityFromNetworkId(netId)
    if not vehicle or vehicle == 0 or not DoesEntityExist(vehicle) then
        return
    end

    local owner = NetworkGetEntityOwner(vehicle)
    if owner ~= source then
        return
    end

    local normalized = normalizeState(state)
    if not normalized then
        return
    end

    Entity(vehicle).state:set(DLSV2.StateBagKey, normalized, true)
end)
