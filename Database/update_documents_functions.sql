-- Add the column to the table
ALTER TABLE stf.documents ADD COLUMN is_visible_to_player BOOLEAN NOT NULL DEFAULT false;

-- Drop existing functions
DROP FUNCTION IF EXISTS stf.fn_documents_insert(character varying, character varying, character varying, timestamp without time zone, timestamp without time zone, bytea, character varying, character varying, character varying, character varying);
DROP FUNCTION IF EXISTS stf.fn_documents_update(character varying, character varying, character varying, timestamp without time zone, bytea, character varying, character varying, character varying, character varying);
DROP FUNCTION IF EXISTS stf.fn_documents_get_all();
DROP FUNCTION IF EXISTS stf.fn_documents_get_by_id(character varying);

-- Create updated insert function
CREATE FUNCTION stf.fn_documents_insert(
    p_document_id character varying, 
    p_document_name character varying, 
    p_document_type character varying, 
    p_document_date timestamp without time zone, 
    p_created_at timestamp without time zone, 
    p_file_data bytea DEFAULT NULL::bytea, 
    p_player_id character varying DEFAULT NULL::character varying, 
    p_club_id character varying DEFAULT NULL::character varying, 
    p_file_size_label character varying DEFAULT NULL::character varying, 
    p_file_extension character varying DEFAULT NULL::character varying, 
    p_is_visible_to_player boolean DEFAULT false
) RETURNS void
    LANGUAGE sql
    AS $$
    INSERT INTO stf.documents (
        document_id,
        player_id,
        club_id,
        document_name,
        document_type,
        document_date,
        file_size_label,
        file_data,
        file_extension,
        created_at,
        is_visible_to_player
    )
    VALUES (
        p_document_id,
        p_player_id,
        p_club_id,
        p_document_name,
        p_document_type,
        p_document_date,
        p_file_size_label,
        p_file_data,
        p_file_extension,
        p_created_at,
        p_is_visible_to_player
    );
$$;

-- Create updated update function
CREATE FUNCTION stf.fn_documents_update(
    p_document_id character varying, 
    p_document_name character varying, 
    p_document_type character varying, 
    p_document_date timestamp without time zone, 
    p_file_data bytea DEFAULT NULL::bytea, 
    p_player_id character varying DEFAULT NULL::character varying, 
    p_club_id character varying DEFAULT NULL::character varying, 
    p_file_size_label character varying DEFAULT NULL::character varying, 
    p_file_extension character varying DEFAULT NULL::character varying, 
    p_is_visible_to_player boolean DEFAULT NULL
) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_affected INTEGER;
BEGIN
    UPDATE stf.documents
    SET
        player_id       = COALESCE(p_player_id, player_id),
        club_id         = COALESCE(p_club_id, club_id),
        document_name   = COALESCE(p_document_name, document_name),
        document_type   = COALESCE(p_document_type, document_type),
        document_date   = COALESCE(p_document_date, document_date),
        file_size_label = COALESCE(p_file_size_label, file_size_label),
        file_data       = COALESCE(p_file_data, file_data),
        file_extension  = COALESCE(p_file_extension, file_extension),
        is_visible_to_player = COALESCE(p_is_visible_to_player, is_visible_to_player)
    WHERE document_id = p_document_id;

    GET DIAGNOSTICS v_affected = ROW_COUNT;
    RETURN v_affected;
END;
$$;

-- Create updated get functions
CREATE FUNCTION stf.fn_documents_get_all() RETURNS TABLE(
    document_id character varying, 
    player_id character varying, 
    club_id character varying, 
    document_name character varying, 
    document_type character varying, 
    document_date timestamp without time zone, 
    file_size_label character varying, 
    file_data bytea, 
    file_extension character varying, 
    created_at timestamp without time zone, 
    is_visible_to_player boolean
)
    LANGUAGE sql STABLE
    AS $$
    SELECT
        document_id,
        player_id,
        club_id,
        document_name,
        document_type,
        document_date,
        file_size_label,
        file_data,
        file_extension,
        created_at,
        is_visible_to_player
    FROM stf.documents
    ORDER BY created_at DESC;
$$;

CREATE FUNCTION stf.fn_documents_get_by_id(p_id character varying) RETURNS TABLE(
    document_id character varying, 
    player_id character varying, 
    club_id character varying, 
    document_name character varying, 
    document_type character varying, 
    document_date timestamp without time zone, 
    file_size_label character varying, 
    file_data bytea, 
    file_extension character varying, 
    created_at timestamp without time zone, 
    is_visible_to_player boolean
)
    LANGUAGE sql STABLE
    AS $$
    SELECT
        document_id,
        player_id,
        club_id,
        document_name,
        document_type,
        document_date,
        file_size_label,
        file_data,
        file_extension,
        created_at,
        is_visible_to_player
    FROM stf.documents
    WHERE document_id = p_id;
$$;