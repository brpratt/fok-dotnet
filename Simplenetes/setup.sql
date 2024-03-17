CREATE TABLE container (
	id serial primary key,
	name text,
	image text
);

ALTER TABLE container
ADD CONSTRAINT unique_name UNIQUE (name);

CREATE INDEX idx_name ON container (name);
