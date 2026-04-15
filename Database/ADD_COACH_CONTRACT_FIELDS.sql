-- ============================================================================
-- Add Coach Contract Date Fields to Players
-- Database: PostgreSQL
-- Purpose: Add contract_start_with_coach and contract_end_with_coach columns
-- Date: April 10, 2026
-- ============================================================================

-- Step 1: Add the new columns to stf.players table
ALTER TABLE stf.players ADD COLUMN IF NOT EXISTS contract_start_with_coach DATE;
ALTER TABLE stf.players ADD COLUMN IF NOT EXISTS contract_end_with_coach DATE;

-- ============================================================================
-- Step 2: Update sp_players_get_all function
-- ============================================================================
DROP FUNCTION IF EXISTS stf.sp_players_get_all();

CREATE FUNCTION stf.sp_players_get_all() 
RETURNS TABLE(
    player_id text, 
    full_name text, 
    date_of_birth date, 
    nationality text, 
    position_code text, 
    preferred_foot text, 
    height_cm integer, 
    weight_kg integer, 
    current_club_id text, 
    contract_start_date date, 
    contract_end_date date, 
    agent_name text, 
    agent_scout_id text, 
    contact_info text, 
    profile_image_url text, 
    sport_id integer,
    contract_start_with_coach date,
    contract_end_with_coach date,
    player_email text, 
    created_at timestamp without time zone, 
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        p.player_id,
        p.full_name,
        p.date_of_birth,
        p.nationality,
        p.position_code,
        p.preferred_foot,
        p.height_cm,
        p.weight_kg,
        p.current_club_id,
        p.contract_start_date,
        p.contract_end_date,
        p.agent_name,
        p.agent_scout_id,
        p.contact_info,
        p.profile_image_url,
        p.sport_id,
        p.contract_start_with_coach,
        p.contract_end_with_coach,
        p.player_email,
        p.created_at,
        p.updated_at
    FROM stf.players p;
END;
$$;

-- ============================================================================
-- Step 3: Update sp_players_get_by_id function
-- ============================================================================
DROP FUNCTION IF EXISTS stf.sp_players_get_by_id(bigint);

CREATE FUNCTION stf.sp_players_get_by_id(p_id bigint) 
RETURNS TABLE(
    player_id text, 
    full_name text, 
    date_of_birth date, 
    nationality text, 
    position_code text, 
    preferred_foot text, 
    height_cm integer, 
    weight_kg integer, 
    current_club_id text, 
    contract_start_date date, 
    contract_end_date date, 
    agent_name text, 
    agent_scout_id text, 
    contact_info text, 
    profile_image_url text, 
    sport_id integer,
    contract_start_with_coach date,
    contract_end_with_coach date,
    player_email text, 
    created_at timestamp without time zone, 
    updated_at timestamp without time zone
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        p.player_id,
        p.full_name,
        p.date_of_birth,
        p.nationality,
        p.position_code,
        p.preferred_foot,
        p.height_cm,
        p.weight_kg,
        p.current_club_id,
        p.contract_start_date,
        p.contract_end_date,
        p.agent_name,
        p.agent_scout_id,
        p.contact_info,
        p.profile_image_url,
        p.sport_id,
        p.contract_start_with_coach,
        p.contract_end_with_coach,
        p.player_email,
        p.created_at,
        p.updated_at
    FROM stf.players p
    WHERE CAST(p.player_id AS BIGINT) = p_id;
END;
$$;

-- ============================================================================
-- Step 4: Update sp_players_insert function
-- ============================================================================
DROP FUNCTION IF EXISTS stf.sp_players_insert(text, text, date, text, text, text, integer, integer, text, date, date, text, text, text, text, integer, timestamp with time zone, timestamp with time zone, text);

CREATE FUNCTION stf.sp_players_insert(
    p_player_id text, 
    p_full_name text, 
    p_date_of_birth date, 
    p_nationality text, 
    p_position_code text, 
    p_preferred_foot text, 
    p_height_cm integer, 
    p_weight_kg integer, 
    p_current_club_id text, 
    p_contract_start_date date, 
    p_contract_end_date date, 
    p_agent_name text, 
    p_agent_scout_id text, 
    p_contact_info text, 
    p_profile_image_url text, 
    p_sport_id integer,
    p_contract_start_with_coach date,
    p_contract_end_with_coach date,
    p_created_at timestamp with time zone, 
    p_updated_at timestamp with time zone, 
    p_player_email text
) 
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO stf.players(
        player_id,
        full_name,
        date_of_birth,
        nationality,
        position_code,
        preferred_foot,
        height_cm,
        weight_kg,
        current_club_id,
        contract_start_date,
        contract_end_date,
        agent_name,
        agent_scout_id,
        contact_info,
        profile_image_url,
        sport_id,
        contract_start_with_coach,
        contract_end_with_coach,
        created_at,
        updated_at,
        player_email
    )
    VALUES (
        p_player_id,
        p_full_name,
        p_date_of_birth,
        p_nationality,
        p_position_code,
        p_preferred_foot,
        p_height_cm,
        p_weight_kg,
        p_current_club_id,
        p_contract_start_date,
        p_contract_end_date,
        p_agent_name,
        p_agent_scout_id,
        p_contact_info,
        p_profile_image_url,
        p_sport_id,
        p_contract_start_with_coach,
        p_contract_end_with_coach,
        p_created_at,
        p_updated_at,
        p_player_email
    );
END;
$$;

-- ============================================================================
-- Step 5: Update sp_players_update function
-- ============================================================================
DROP FUNCTION IF EXISTS stf.sp_players_update(text, text, date, text, text, text, integer, integer, text, date, date, text, text, text, text, integer, timestamp with time zone);

CREATE FUNCTION stf.sp_players_update(
    p_player_id text, 
    p_full_name text, 
    p_date_of_birth date, 
    p_nationality text, 
    p_position_code text, 
    p_preferred_foot text, 
    p_height_cm integer, 
    p_weight_kg integer, 
    p_current_club_id text, 
    p_contract_start_date date, 
    p_contract_end_date date, 
    p_agent_name text, 
    p_agent_scout_id text, 
    p_contact_info text, 
    p_profile_image_url text, 
    p_sport_id integer,
    p_contract_start_with_coach date,
    p_contract_end_with_coach date,
    p_updated_at timestamp with time zone
) 
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE stf.players
    SET
        full_name = p_full_name,
        date_of_birth = p_date_of_birth,
        nationality = p_nationality,
        position_code = p_position_code,
        preferred_foot = p_preferred_foot,
        height_cm = p_height_cm,
        weight_kg = p_weight_kg,
        current_club_id = p_current_club_id,
        contract_start_date = p_contract_start_date,
        contract_end_date = p_contract_end_date,
        agent_name = p_agent_name,
        agent_scout_id = p_agent_scout_id,
        contact_info = p_contact_info,
        profile_image_url = p_profile_image_url,
        sport_id = p_sport_id,
        contract_start_with_coach = p_contract_start_with_coach,
        contract_end_with_coach = p_contract_end_with_coach,
        updated_at = p_updated_at
    WHERE player_id = p_player_id;
END;
$$;

-- ============================================================================
-- Verification Queries
-- ============================================================================

-- Verify columns exist
SELECT column_name, data_type
FROM information_schema.columns 
WHERE table_schema = 'stf' 
AND table_name = 'players' 
AND column_name IN ('contract_start_with_coach', 'contract_end_with_coach');

-- Verify function signatures
SELECT proname, pg_get_function_identity_arguments(oid) as args
FROM pg_proc 
WHERE proname LIKE 'sp_players_%' 
AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'stf')
ORDER BY proname;
