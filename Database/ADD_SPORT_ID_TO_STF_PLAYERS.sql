-- ============================================================================
-- Add Sport ID to STF Players - Database Schema Update Script
-- Database: PostgreSQL
-- Purpose: Add sport_id column and update stored procedures to handle sport assignment
-- Date: April 10, 2026
-- ============================================================================

BEGIN TRANSACTION;

-- Add sport_id column to stf.players table if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'stf' 
                   AND table_name = 'players' 
                   AND column_name = 'sport_id') THEN
        ALTER TABLE stf.players 
        ADD COLUMN sport_id INTEGER REFERENCES stf.sports(id);
        
        -- Add comment
        COMMENT ON COLUMN stf.sports.sport_id IS 'Reference to the sport this player participates in';
    END IF;
END $$;

-- Update sp_players_get_all to include sport_id
CREATE OR REPLACE FUNCTION stf.sp_players_get_all() 
RETURNS TABLE(
    player_id character varying, 
    full_name character varying, 
    date_of_birth date, 
    nationality character varying, 
    position_code character varying, 
    preferred_foot character varying, 
    height_cm integer, 
    weight_kg integer, 
    current_club_id character varying, 
    contract_start_date date, 
    contract_end_date date, 
    agent_name character varying, 
    agent_scout_id character varying, 
    contact_info character varying, 
    profile_image_url character varying, 
    player_email character varying, 
    sport_id integer,
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
    p.player_email,
    p.sport_id,
    p.created_at,
    p.updated_at
FROM stf.players p;

END;
$$;

-- Update sp_players_get_by_id to include sport_id
CREATE OR REPLACE FUNCTION stf.sp_players_get_by_id(p_id bigint) 
RETURNS TABLE(
    player_id character varying, 
    full_name character varying, 
    date_of_birth date, 
    nationality character varying, 
    position_code character varying, 
    preferred_foot character varying, 
    height_cm integer, 
    weight_kg integer, 
    current_club_id character varying, 
    contract_start_date date, 
    contract_end_date date, 
    agent_name character varying, 
    agent_scout_id character varying, 
    contact_info character varying, 
    profile_image_url character varying, 
    player_email character varying, 
    sport_id integer,
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
    p.player_email,
    p.sport_id,
    p.created_at,
    p.updated_at
FROM stf.players p
WHERE p.player_id::BIGINT = p_id;

END;
$$;

-- Update sp_players_insert to include sport_id
CREATE OR REPLACE FUNCTION stf.sp_players_insert(
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
    p_created_at timestamp with time zone, 
    p_updated_at timestamp with time zone, 
    p_player_email text
) RETURNS void
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
    p_created_at,
    p_updated_at,
    p_player_email
);

END;
$$;

-- Update sp_players_update to include sport_id
CREATE OR REPLACE FUNCTION stf.sp_players_update(
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
    p_updated_at timestamp with time zone
) RETURNS void
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
    updated_at = p_updated_at
WHERE player_id = p_player_id;

END;
$$;

COMMIT TRANSACTION;

-- ============================================================================
-- Verification Queries
-- ============================================================================

-- Verify column was added
SELECT column_name, data_type, is_nullable, column_default 
FROM information_schema.columns 
WHERE table_schema = 'stf' 
AND table_name = 'players' 
AND column_name = 'sport_id';

-- Check updated function signatures
SELECT proname, pg_get_function_identity_arguments(oid) as args
FROM pg_proc 
WHERE proname LIKE 'sp_players_%' 
AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'stf');

-- ============================================================================
-- Helper Queries (Optional)
-- ============================================================================

-- Update existing players to have a default sport_id if needed
-- UPDATE stf.players SET sport_id = 1 WHERE sport_id IS NULL; -- Uncomment and modify as needed