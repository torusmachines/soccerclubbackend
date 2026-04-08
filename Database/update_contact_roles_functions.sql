-- Update contact_roles functions to include description column
-- Run this script against your PostgreSQL database

-- Drop existing functions
DROP FUNCTION IF EXISTS stf.fn_contact_roles_get_all();
DROP FUNCTION IF EXISTS stf.fn_contact_roles_get_by_id(character varying);
DROP FUNCTION IF EXISTS stf.fn_contact_roles_get_by_name(character varying);
DROP FUNCTION IF EXISTS stf.fn_contact_roles_insert(character varying, character varying, timestamp without time zone, character varying);
DROP FUNCTION IF EXISTS stf.fn_contact_roles_update(character varying, character varying);

-- Recreate functions with description column support

-- Name: fn_contact_roles_get_all(); Type: FUNCTION; Schema: stf; Owner: postgres
CREATE FUNCTION stf.fn_contact_roles_get_all() RETURNS TABLE(role_id character varying, role_name character varying, description text, created_at timestamp without time zone, created_by character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT
        role_id,
        role_name,
        description,
        created_at,
        created_by
    FROM stf.contact_roles
    ORDER BY role_name;
$$;

ALTER FUNCTION stf.fn_contact_roles_get_all() OWNER TO postgres;

-- Name: fn_contact_roles_get_by_id(character varying); Type: FUNCTION; Schema: stf; Owner: postgres
CREATE FUNCTION stf.fn_contact_roles_get_by_id(p_id character varying) RETURNS TABLE(role_id character varying, role_name character varying, description text, created_at timestamp without time zone, created_by character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT
        role_id,
        role_name,
        description,
        created_at,
        created_by
    FROM stf.contact_roles
    WHERE role_id = p_id;
$$;

ALTER FUNCTION stf.fn_contact_roles_get_by_id(p_id character varying) OWNER TO postgres;

-- Name: fn_contact_roles_get_by_name(character varying); Type: FUNCTION; Schema: stf; Owner: postgres
CREATE FUNCTION stf.fn_contact_roles_get_by_name(p_name character varying) RETURNS TABLE(role_id character varying, role_name character varying, description text, created_at timestamp without time zone, created_by character varying)
    LANGUAGE sql STABLE
    AS $$
    SELECT
        role_id,
        role_name,
        description,
        created_at,
        created_by
    FROM stf.contact_roles
    WHERE role_name = p_name;
$$;

ALTER FUNCTION stf.fn_contact_roles_get_by_name(p_name character varying) OWNER TO postgres;

-- Name: fn_contact_roles_insert(character varying, character varying, text, timestamp without time zone, character varying); Type: FUNCTION; Schema: stf; Owner: postgres
CREATE FUNCTION stf.fn_contact_roles_insert(p_role_id character varying, p_role_name character varying, p_description text, p_created_at timestamp without time zone, p_created_by character varying) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO stf.contact_roles (
        role_id,
        role_name,
        description,
        created_at,
        created_by
    ) VALUES (
        p_role_id,
        p_role_name,
        p_description,
        p_created_at,
        p_created_by
    );
END;
$$;

ALTER FUNCTION stf.fn_contact_roles_insert(p_role_id character varying, p_role_name character varying, p_description text, p_created_at timestamp without time zone, p_created_by character varying) OWNER TO postgres;

-- Name: fn_contact_roles_update(character varying, character varying, text); Type: FUNCTION; Schema: stf; Owner: postgres
CREATE FUNCTION stf.fn_contact_roles_update(p_role_id character varying, p_role_name character varying, p_description text) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_affected INTEGER;
BEGIN
    UPDATE stf.contact_roles
    SET role_name = p_role_name,
        description = p_description
    WHERE role_id = p_role_id;

    GET DIAGNOSTICS v_affected = ROW_COUNT;
    RETURN v_affected;
END;
$$;

ALTER FUNCTION stf.fn_contact_roles_update(p_role_id character varying, p_role_name character varying, p_description text) OWNER TO postgres;