-- ============================================================
-- Create Player Positions Table and Functions
-- ============================================================

-- CREATE TABLE stf.player_positions
CREATE TABLE IF NOT EXISTS stf.player_positions (
    position_id character varying(50) NOT NULL PRIMARY KEY,
    position_code character varying(10) NOT NULL UNIQUE,
    position_name character varying(100) NOT NULL,
    description text,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    created_by character varying(50) NOT NULL
);

ALTER TABLE stf.player_positions OWNER TO postgres;


-- ============================================================
-- DROP existing functions
-- ============================================================
DROP FUNCTION IF EXISTS stf.fn_player_positions_delete(character varying);
DROP FUNCTION IF EXISTS stf.fn_player_positions_get_all();
DROP FUNCTION IF EXISTS stf.fn_player_positions_get_by_id(character varying);
DROP FUNCTION IF EXISTS stf.fn_player_positions_get_by_code(character varying);
DROP FUNCTION IF EXISTS stf.fn_player_positions_insert(character varying, character varying, character varying, text, timestamp without time zone, character varying);
DROP FUNCTION IF EXISTS stf.fn_player_positions_update(character varying, character varying, character varying, text);


-- ============================================================
-- CREATE Player Position Functions
-- ============================================================

-- fn_player_positions_get_all - Get all player positions
CREATE FUNCTION stf.fn_player_positions_get_all() 
RETURNS TABLE(position_id character varying, position_code character varying, position_name character varying, description text, created_at timestamp without time zone, created_by character varying)
LANGUAGE sql STABLE
AS $$
    SELECT
        position_id,
        position_code,
        position_name,
        description,
        created_at,
        created_by
    FROM stf.player_positions
    ORDER BY position_code;
$$;

ALTER FUNCTION stf.fn_player_positions_get_all() OWNER TO postgres;


-- fn_player_positions_get_by_id - Get player position by ID
CREATE FUNCTION stf.fn_player_positions_get_by_id(p_id character varying) 
RETURNS TABLE(position_id character varying, position_code character varying, position_name character varying, description text, created_at timestamp without time zone, created_by character varying)
LANGUAGE sql STABLE
AS $$
    SELECT
        position_id,
        position_code,
        position_name,
        description,
        created_at,
        created_by
    FROM stf.player_positions
    WHERE position_id = p_id;
$$;

ALTER FUNCTION stf.fn_player_positions_get_by_id(p_id character varying) OWNER TO postgres;


-- fn_player_positions_get_by_code - Get player position by code
CREATE FUNCTION stf.fn_player_positions_get_by_code(p_code character varying) 
RETURNS TABLE(position_id character varying, position_code character varying, position_name character varying, description text, created_at timestamp without time zone, created_by character varying)
LANGUAGE sql STABLE
AS $$
    SELECT
        position_id,
        position_code,
        position_name,
        description,
        created_at,
        created_by
    FROM stf.player_positions
    WHERE position_code = p_code;
$$;

ALTER FUNCTION stf.fn_player_positions_get_by_code(p_code character varying) OWNER TO postgres;


-- fn_player_positions_insert - Insert new player position
CREATE FUNCTION stf.fn_player_positions_insert(p_position_id character varying, p_position_code character varying, p_position_name character varying, p_description text, p_created_at timestamp without time zone, p_created_by character varying) 
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO stf.player_positions (
        position_id,
        position_code,
        position_name,
        description,
        created_at,
        created_by
    ) VALUES (
        p_position_id,
        p_position_code,
        p_position_name,
        p_description,
        p_created_at,
        p_created_by
    );
END;
$$;

ALTER FUNCTION stf.fn_player_positions_insert(p_position_id character varying, p_position_code character varying, p_position_name character varying, p_description text, p_created_at timestamp without time zone, p_created_by character varying) OWNER TO postgres;


-- fn_player_positions_update - Update player position
CREATE FUNCTION stf.fn_player_positions_update(p_position_id character varying, p_position_code character varying, p_position_name character varying, p_description text) 
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_affected INTEGER;
BEGIN
    UPDATE stf.player_positions
    SET position_code = p_position_code,
        position_name = p_position_name,
        description = p_description
    WHERE position_id = p_position_id;

    GET DIAGNOSTICS v_affected = ROW_COUNT;
    RETURN v_affected;
END;
$$;

ALTER FUNCTION stf.fn_player_positions_update(p_position_id character varying, p_position_code character varying, p_position_name character varying, p_description text) OWNER TO postgres;


-- fn_player_positions_delete - Delete player position
CREATE FUNCTION stf.fn_player_positions_delete(p_id character varying) 
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_affected INTEGER;
BEGIN
    DELETE FROM stf.player_positions
    WHERE position_id = p_id;

    GET DIAGNOSTICS v_affected = ROW_COUNT;
    RETURN v_affected;
END;
$$;

ALTER FUNCTION stf.fn_player_positions_delete(p_id character varying) OWNER TO postgres;


-- ============================================================
-- INSERT Default Player Positions
-- ============================================================
INSERT INTO stf.player_positions (position_id, position_code, position_name, description, created_at, created_by)
VALUES 
    (gen_random_uuid()::text, 'GK', 'Goalkeeper', 'Goalkeeper - Protects the goal', CURRENT_TIMESTAMP, 'system'),
    (gen_random_uuid()::text, 'CB', 'Center Back', 'Defender - Plays in the center of defense', CURRENT_TIMESTAMP, 'system'),
    (gen_random_uuid()::text, 'RB', 'Right Back', 'Defender - Plays on the right side of defense', CURRENT_TIMESTAMP, 'system'),
    (gen_random_uuid()::text, 'LB', 'Left Back', 'Defender - Plays on the left side of defense', CURRENT_TIMESTAMP, 'system'),
    (gen_random_uuid()::text, 'CDM', 'Central Defensive Midfielder', 'Midfielder - Defensive midfielder in the center', CURRENT_TIMESTAMP, 'system'),
    (gen_random_uuid()::text, 'CM', 'Central Midfielder', 'Midfielder - Plays in the center of midfield', CURRENT_TIMESTAMP, 'system'),
    (gen_random_uuid()::text, 'CAM', 'Central Attacking Midfielder', 'Midfielder - Attacking midfielder in the center', CURRENT_TIMESTAMP, 'system'),
    (gen_random_uuid()::text, 'RW', 'Right Winger', 'Forward - Plays on the right wing', CURRENT_TIMESTAMP, 'system'),
    (gen_random_uuid()::text, 'LW', 'Left Winger', 'Forward - Plays on the left wing', CURRENT_TIMESTAMP, 'system'),
    (gen_random_uuid()::text, 'CF', 'Center Forward', 'Forward - Plays in the center of attack', CURRENT_TIMESTAMP, 'system'),
    (gen_random_uuid()::text, 'ST', 'Striker', 'Forward - Main striker/goal scorer', CURRENT_TIMESTAMP, 'system')
ON CONFLICT DO NOTHING;


-- ============================================================
-- VERIFY
-- ============================================================
SELECT * FROM stf.player_positions ORDER BY position_code;
