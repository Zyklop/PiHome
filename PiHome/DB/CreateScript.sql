SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET client_min_messages = warning;
SET row_security = off;

CREATE USER writer WITH
  LOGIN
  NOSUPERUSER
  INHERIT
  CREATEDB
  NOCREATEROLE
  REPLICATION
  PASSWORD 'PiWriter';

CREATE USER reader WITH
  LOGIN
  NOSUPERUSER
  INHERIT
  NOCREATEDB
  NOCREATEROLE
  NOREPLICATION
  PASSWORD 'PiReader';

CREATE DATABASE "PiHome";


ALTER DATABASE "PiHome" OWNER TO writer;

\connect "PiHome"

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET client_min_messages = warning;
SET row_security = off;

CREATE EXTENSION IF NOT EXISTS plpgsql WITH SCHEMA pg_catalog;

COMMENT ON EXTENSION plpgsql IS 'PL/pgSQL procedural language';


SET default_tablespace = '';

SET default_with_oids = false;

CREATE TABLE public."Feature" (
    "Id" integer NOT NULL,
    "Name" character varying(30) NOT NULL,
    "Unit" character varying(20) NOT NULL,
    "LogFactor" double precision NOT NULL
);

ALTER TABLE public."Feature" OWNER TO writer;

CREATE TABLE public."Led" (
    "Index" integer NOT NULL,
    "ModuleId" integer NOT NULL,
    "Position" point NOT NULL,
    "Id" serial NOT NULL
);

ALTER TABLE public."Led" OWNER TO writer;

CREATE TABLE public."LedPreset" (
    "Id" serial NOT NULL,
    "Name" character varying(30) NOT NULL,
    "ChangeDate" timestamp without time zone NOT NULL
);

ALTER TABLE public."LedPreset" OWNER TO writer;

CREATE TABLE public."LedPresetValues" (
    "Id" serial NOT NULL,
    "LedId" integer NOT NULL,
    "PresetId" integer NOT NULL,
    "Color" bytea
);

ALTER TABLE public."LedPresetValues" OWNER TO writer;

CREATE TABLE public."Log" (
    "Time" timestamp without time zone NOT NULL,
    "Value" integer NOT NULL,
    "Id" bigserial NOT NULL,
    "LogConfigurationId" integer NOT NULL
);

ALTER TABLE public."Log" OWNER TO writer;

CREATE TABLE public."LogConfiguration" (
    "ModuleId" integer NOT NULL,
    "FeatureId" integer NOT NULL,
    "Interval" interval NOT NULL,
    "NextPoll" timestamp without time zone NOT NULL,
    "Id" serial NOT NULL,
    "RetensionTime" interval
);

ALTER TABLE public."LogConfiguration" OWNER TO writer;

CREATE TABLE public."Module" (
    "Id" serial NOT NULL,
    "Name" character varying(30) NOT NULL,
    "Ip" inet NOT NULL,
    "FeatureIds" integer[] NOT NULL
);

ALTER TABLE public."Module" OWNER TO writer;

ALTER TABLE ONLY public."Feature"
    ADD CONSTRAINT "Feature_pkey" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."LedPresetValues"
    ADD CONSTRAINT "LedPresetValues_pkey" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."LedPreset"
    ADD CONSTRAINT "LedPreset_pkey" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."Led"
    ADD CONSTRAINT "Led_pkey" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."LogConfiguration"
    ADD CONSTRAINT "LogConfiguration_pkey" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."Log"
    ADD CONSTRAINT "Log_pkey" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."Module"
    ADD CONSTRAINT "Module_pkey" PRIMARY KEY ("Id");

ALTER TABLE ONLY public."Module"
    ADD CONSTRAINT "UC_Ip" UNIQUE ("Ip");

ALTER TABLE ONLY public."Led"
    ADD CONSTRAINT "UC_Key" UNIQUE ("Index", "ModuleId");

ALTER TABLE ONLY public."LogConfiguration"
    ADD CONSTRAINT "UC_LogSetting" UNIQUE ("ModuleId", "FeatureId");

ALTER TABLE ONLY public."Module"
    ADD CONSTRAINT "UC_Module_Name" UNIQUE ("Name");

ALTER TABLE ONLY public."LedPreset"
    ADD CONSTRAINT "UC_Name" UNIQUE ("Name");

ALTER TABLE ONLY public."LedPresetValues"
    ADD CONSTRAINT "UC_PresetId_LedId" UNIQUE ("LedId", "PresetId");

CREATE INDEX "IX_Module_Feature" ON public."LogConfiguration" USING btree ("ModuleId", "FeatureId");

CREATE INDEX "IX_Name" ON public."LedPreset" USING btree ("Name");

CREATE INDEX "IX_Preset" ON public."LedPresetValues" USING btree ("PresetId");

CREATE INDEX "IX_Time" ON public."Log" USING btree ("Time" DESC NULLS LAST, "LogConfigurationId");

ALTER TABLE ONLY public."LogConfiguration"
    ADD CONSTRAINT "FK_Feature" FOREIGN KEY ("FeatureId") REFERENCES public."Feature"("Id");

ALTER TABLE ONLY public."LedPresetValues"
    ADD CONSTRAINT "FK_Led" FOREIGN KEY ("LedId") REFERENCES public."Led"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."Log"
    ADD CONSTRAINT "FK_LogConfiguration" FOREIGN KEY ("LogConfigurationId") REFERENCES public."LogConfiguration"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."Led"
    ADD CONSTRAINT "FK_Module" FOREIGN KEY ("ModuleId") REFERENCES public."Module"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."LogConfiguration"
    ADD CONSTRAINT "FK_Module" FOREIGN KEY ("ModuleId") REFERENCES public."Module"("Id") ON DELETE CASCADE;

ALTER TABLE ONLY public."LedPresetValues"
    ADD CONSTRAINT "FK_Preset" FOREIGN KEY ("PresetId") REFERENCES public."LedPreset"("Id") ON DELETE CASCADE;

INSERT INTO public."Feature"(
	"Id", "Name", "Unit", "LogFactor")
	VALUES (1, 'Temperature', '°C', 100.0);

INSERT INTO public."Feature"(
	"Id", "Name", "Unit", "LogFactor")
	VALUES (2, 'Humidity', '%', 100.0);

INSERT INTO public."Feature"(
	"Id", "Name", "Unit", "LogFactor")
	VALUES (3, 'AirQuality', '', 1.0);

INSERT INTO public."Feature"(
	"Id", "Name", "Unit", "LogFactor")
	VALUES (4, 'Lunimosity', '', 1.0);

