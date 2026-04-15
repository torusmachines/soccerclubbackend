-- Add new review activity ratings table to replace stf.review_ratings
-- This table stores one rating row per review + sport activity combination.

CREATE SCHEMA IF NOT EXISTS stf;

CREATE TABLE IF NOT EXISTS stf.review_activity_ratings (
    review_activity_rating_id SERIAL PRIMARY KEY,
    review_id VARCHAR(50) NOT NULL,
    activity_id INT NOT NULL,
    rating NUMERIC(3,1) NOT NULL,
    comment TEXT NULL,
    rating_followup_date TIMESTAMP NULL,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_review_activity_ratings_review
        FOREIGN KEY (review_id)
        REFERENCES stf.reviews(review_id)
        ON DELETE CASCADE,
    CONSTRAINT fk_review_activity_ratings_activity
        FOREIGN KEY (activity_id)
        REFERENCES stf.sport_activities(activity_id)
        ON DELETE CASCADE,
    CONSTRAINT uq_review_activity_ratings_review_activity
        UNIQUE (review_id, activity_id),
    CONSTRAINT chk_review_activity_ratings_rating_range
        CHECK (rating >= 1 AND rating <= 5)
);

-- Optional helper functions for review activity ratings
CREATE OR REPLACE FUNCTION stf.fn_review_activity_ratings_get_all()
RETURNS TABLE(
    review_activity_rating_id INT,
    review_id VARCHAR,
    activity_id INT,
    rating NUMERIC(3,1),
    comment TEXT,
    rating_followup_date TIMESTAMP,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
)
LANGUAGE sql STABLE
AS $$
SELECT
    review_activity_rating_id,
    review_id,
    activity_id,
    rating,
    comment,
    rating_followup_date,
    created_at,
    updated_at
FROM stf.review_activity_ratings
ORDER BY review_activity_rating_id;
$$;

CREATE OR REPLACE FUNCTION stf.fn_review_activity_ratings_get_by_id(p_id INT)
RETURNS TABLE(
    review_activity_rating_id INT,
    review_id VARCHAR,
    activity_id INT,
    rating NUMERIC(3,1),
    comment TEXT,
    rating_followup_date TIMESTAMP,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
)
LANGUAGE sql STABLE
AS $$
SELECT
    review_activity_rating_id,
    review_id,
    activity_id,
    rating,
    comment,
    rating_followup_date,
    created_at,
    updated_at
FROM stf.review_activity_ratings
WHERE review_activity_rating_id = p_id;
$$;

CREATE OR REPLACE FUNCTION stf.fn_review_activity_ratings_get_by_review_id(p_review_id VARCHAR)
RETURNS TABLE(
    review_activity_rating_id INT,
    review_id VARCHAR,
    activity_id INT,
    rating NUMERIC(3,1),
    comment TEXT,
    rating_followup_date TIMESTAMP,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
)
LANGUAGE sql STABLE
AS $$
SELECT
    review_activity_rating_id,
    review_id,
    activity_id,
    rating,
    comment,
    rating_followup_date,
    created_at,
    updated_at
FROM stf.review_activity_ratings
WHERE review_id = p_review_id
ORDER BY activity_id;
$$;

CREATE OR REPLACE FUNCTION stf.fn_review_activity_ratings_insert(
    p_review_id VARCHAR,
    p_activity_id INT,
    p_rating NUMERIC(3,1),
    p_comment TEXT DEFAULT NULL,
    p_rating_followup_date TIMESTAMP DEFAULT NULL
)
RETURNS VOID
LANGUAGE sql
AS $$
INSERT INTO stf.review_activity_ratings (
    review_id,
    activity_id,
    rating,
    comment,
    rating_followup_date
)
VALUES (
    p_review_id,
    p_activity_id,
    p_rating,
    p_comment,
    p_rating_followup_date
);
$$;

CREATE OR REPLACE FUNCTION stf.fn_review_activity_ratings_update(
    p_review_activity_rating_id INT,
    p_rating NUMERIC(3,1),
    p_comment TEXT DEFAULT NULL,
    p_rating_followup_date TIMESTAMP DEFAULT NULL
)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_affected INTEGER;
BEGIN
    UPDATE stf.review_activity_ratings
    SET
        rating = p_rating,
        comment = p_comment,
        rating_followup_date = p_rating_followup_date,
        updated_at = CURRENT_TIMESTAMP
    WHERE review_activity_rating_id = p_review_activity_rating_id;

    GET DIAGNOSTICS v_affected = ROW_COUNT;
    RETURN v_affected;
END;
$$;

CREATE OR REPLACE FUNCTION stf.fn_review_activity_ratings_delete(p_id INT)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_affected INTEGER;
BEGIN
    DELETE FROM stf.review_activity_ratings
    WHERE review_activity_rating_id = p_id;

    GET DIAGNOSTICS v_affected = ROW_COUNT;
    RETURN v_affected;
END;
$$;

CREATE OR REPLACE FUNCTION stf.fn_review_activity_ratings_exists(p_id INT)
RETURNS INTEGER
LANGUAGE sql STABLE
AS $$
SELECT COUNT(1)::INTEGER
FROM stf.review_activity_ratings
WHERE review_activity_rating_id = p_id;
$$;

-- Helper query: get ratings for a specific player by player_id
CREATE OR REPLACE FUNCTION stf.fn_review_activity_ratings_get_by_player_id(p_player_id VARCHAR)
RETURNS TABLE(
    review_activity_rating_id INT,
    review_id VARCHAR,
    player_id VARCHAR,
    activity_id INT,
    activity_name VARCHAR,
    rating NUMERIC(3,1),
    comment TEXT,
    rating_followup_date TIMESTAMP,
    review_match_date TIMESTAMP,
    review_notes TEXT
)
LANGUAGE sql STABLE
AS $$
SELECT
    ar.review_activity_rating_id,
    r.review_id,
    r.player_id,
    ar.activity_id,
    sa.activity_name,
    ar.rating,
    ar.comment,
    ar.rating_followup_date,
    r.match_date AS review_match_date,
    r.notes AS review_notes
FROM stf.review_activity_ratings ar
JOIN stf.reviews r ON ar.review_id = r.review_id
JOIN stf.sport_activities sa ON ar.activity_id = sa.activity_id
WHERE r.player_id = p_player_id
ORDER BY r.match_date DESC, ar.review_activity_rating_id;
$$;

-- Example manual query to show a player's reviews and activity ratings:
-- SELECT * FROM stf.fn_review_activity_ratings_get_by_player_id('PLAYER_ID');
