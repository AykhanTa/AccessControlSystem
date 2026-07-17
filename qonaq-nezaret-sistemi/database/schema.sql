-- =====================================================================
--  Giriş-çıxışa Nəzarət Sistemi — Verilənlər bazası strukturu
--  Dialekt: MySQL 8 / MariaDB 10.4+  (InnoDB, utf8mb4)
--  Qeyd (PostgreSQL): BIGINT AUTO_INCREMENT → BIGSERIAL,
--        ENUM(...) → CREATE TYPE ... AS ENUM, TINYINT(1) → BOOLEAN.
-- =====================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================================
--  A BÖLMƏ — İSTİFADƏÇİLƏR, ROLLAR VƏ İCAZƏLƏR
-- =====================================================================

-- Rollar (Administrator, Mühafizə, Qəbul + xüsusi rollar)
CREATE TABLE roles (
    id           BIGINT       NOT NULL AUTO_INCREMENT,
    name         VARCHAR(80)  NOT NULL,
    description  VARCHAR(255)     NULL,
    is_system    TINYINT(1)   NOT NULL DEFAULT 0,   -- 1 = qorunan (Administrator), silinə/redaktə edilə bilməz
    created_at   TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at   TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_roles_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Sistem bölmələri (icazə matrisinin sətirləri)
CREATE TABLE sections (
    id          BIGINT      NOT NULL AUTO_INCREMENT,
    code        VARCHAR(40) NOT NULL,   -- 'dashboard','guests','cards','history','active_permits','reports','logs','users','roles','settings'
    name        VARCHAR(80) NOT NULL,   -- görünən ad: 'Ana səhifə', 'Qonaqlar' ...
    sort_order  INT         NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_sections_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Rol × Bölmə icazələri (Baxış / Əlavə / Redaktə / Silmə)
CREATE TABLE role_permissions (
    id          BIGINT     NOT NULL AUTO_INCREMENT,
    role_id     BIGINT     NOT NULL,
    section_id  BIGINT     NOT NULL,
    can_view    TINYINT(1) NOT NULL DEFAULT 0,
    can_add     TINYINT(1) NOT NULL DEFAULT 0,
    can_edit    TINYINT(1) NOT NULL DEFAULT 0,
    can_delete  TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_role_section (role_id, section_id),
    CONSTRAINT fk_rp_role    FOREIGN KEY (role_id)    REFERENCES roles(id)    ON DELETE CASCADE,
    CONSTRAINT fk_rp_section FOREIGN KEY (section_id) REFERENCES sections(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Sistem istifadəçiləri (sistemə daxil olanlar)
CREATE TABLE users (
    id             BIGINT       NOT NULL AUTO_INCREMENT,
    first_name     VARCHAR(80)  NOT NULL,
    last_name      VARCHAR(80)  NOT NULL,
    email          VARCHAR(160) NOT NULL,
    password_hash  VARCHAR(255) NOT NULL,            -- bcrypt/argon2 (açıq şifrə SAXLANILMIR)
    role_id        BIGINT       NOT NULL,
    status         ENUM('active','inactive') NOT NULL DEFAULT 'active',
    is_protected   TINYINT(1)   NOT NULL DEFAULT 0,  -- 1 = Global administrator (silinə bilməz)
    last_login_at  TIMESTAMP        NULL,
    created_at     TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at     TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_users_email (email),
    KEY ix_users_role (role_id),
    CONSTRAINT fk_users_role FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================================
--  B BÖLMƏ — BAZA KATALOQLARI (Parametrlər)
-- =====================================================================

-- Qəbul edən şəxslər
CREATE TABLE hosts (
    id          BIGINT       NOT NULL AUTO_INCREMENT,
    first_name  VARCHAR(80)  NOT NULL,
    last_name   VARCHAR(80)  NOT NULL,
    email       VARCHAR(160)     NULL,
    phone       VARCHAR(40)      NULL,
    department  VARCHAR(120)     NULL,   -- Şöbə/vəzifə
    is_active   TINYINT(1)   NOT NULL DEFAULT 1,
    created_at  TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at  TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_hosts_active (is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Giriş əraziləri
CREATE TABLE areas (
    id          BIGINT       NOT NULL AUTO_INCREMENT,
    name        VARCHAR(160) NOT NULL,
    is_active   TINYINT(1)   NOT NULL DEFAULT 1,
    created_at  TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_areas_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Gəliş məqsədləri
CREATE TABLE purposes (
    id          BIGINT       NOT NULL AUTO_INCREMENT,
    name        VARCHAR(120) NOT NULL,
    is_active   TINYINT(1)   NOT NULL DEFAULT 1,
    created_at  TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_purposes_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Müvəqqəti kartlar
CREATE TABLE cards (
    id          BIGINT       NOT NULL AUTO_INCREMENT,
    card_no     VARCHAR(40)  NOT NULL,               -- məs. 'MK-24-00087'
    note        VARCHAR(255)     NULL,
    status      ENUM('free','assigned') NOT NULL DEFAULT 'free',  -- Boş / təyin edilib
    is_active   TINYINT(1)   NOT NULL DEFAULT 1,     -- 0 = deaktiv edilmiş kart
    created_at  TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at  TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_cards_no (card_no),
    KEY ix_cards_status (status, is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================================
--  C BÖLMƏ — QONAQLAR VƏ ZİYARƏTLƏR
-- =====================================================================

-- Qonaq (şəxs) — təkrar ziyarətlərdə eyni qonaq istifadə olunur
CREATE TABLE guests (
    id            BIGINT       NOT NULL AUTO_INCREMENT,
    first_name    VARCHAR(80)  NOT NULL,             -- Ad
    last_name     VARCHAR(80)  NOT NULL,             -- Soyad
    patronymic    VARCHAR(80)      NULL,             -- Atasının adı
    id_document   VARCHAR(40)  NOT NULL,             -- Şəxsiyyət vəsiqəsi
    phone         VARCHAR(40)      NULL,
    email         VARCHAR(160)     NULL,
    photo_path    VARCHAR(255)     NULL,             -- yüklənmiş foto (məs. 'uploads/guests/12.jpg')
    document_path VARCHAR(255)     NULL,             -- əlavə sənəd faylı
    created_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_guests_document (id_document),
    KEY ix_guests_name (last_name, first_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Ziyarət (bir giriş sessiyası)
CREATE TABLE visits (
    id                BIGINT     NOT NULL AUTO_INCREMENT,
    guest_id          BIGINT     NOT NULL,
    host_id           BIGINT     NOT NULL,           -- qəbul edən şəxs
    pass_type         ENUM('card','qr') NOT NULL,    -- buraxılış növü
    card_id           BIGINT         NULL,           -- pass_type='card' olduqda dolur
    qr_token          VARCHAR(64)    NULL,           -- pass_type='qr' olduqda (unikal skan tokeni)
    arrival_at        DATETIME   NOT NULL,           -- gəliş tarixi/vaxtı
    expected_exit_at  DATETIME       NULL,           -- çıxış (proqnoz)
    actual_exit_at    DATETIME       NULL,           -- faktiki çıxış (təsdiqləndikdə)
    status            ENUM('in','out','late') NOT NULL DEFAULT 'in',  -- Binadadır/Çıxıb/Gecikib
    note              VARCHAR(500)   NULL,
    created_by        BIGINT         NULL,           -- ziyarəti qeydə alan istifadəçi
    created_at        TIMESTAMP  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at        TIMESTAMP  NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visits_qr (qr_token),
    KEY ix_visits_guest (guest_id),
    KEY ix_visits_host (host_id),
    KEY ix_visits_status (status),
    KEY ix_visits_arrival (arrival_at),
    CONSTRAINT fk_visits_guest   FOREIGN KEY (guest_id)   REFERENCES guests(id) ON DELETE RESTRICT,
    CONSTRAINT fk_visits_host    FOREIGN KEY (host_id)    REFERENCES hosts(id)  ON DELETE RESTRICT,
    CONSTRAINT fk_visits_card    FOREIGN KEY (card_id)    REFERENCES cards(id)  ON DELETE SET NULL,
    CONSTRAINT fk_visits_creator FOREIGN KEY (created_by) REFERENCES users(id)  ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Ziyarət ↔ Ərazi (çox-çoxa): bir ziyarətə bir neçə ərazi icazəsi
CREATE TABLE visit_areas (
    visit_id  BIGINT NOT NULL,
    area_id   BIGINT NOT NULL,
    PRIMARY KEY (visit_id, area_id),
    CONSTRAINT fk_va_visit FOREIGN KEY (visit_id) REFERENCES visits(id) ON DELETE CASCADE,
    CONSTRAINT fk_va_area  FOREIGN KEY (area_id)  REFERENCES areas(id)  ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Ziyarət ↔ Məqsəd (çox-çoxa)
CREATE TABLE visit_purposes (
    visit_id    BIGINT NOT NULL,
    purpose_id  BIGINT NOT NULL,
    PRIMARY KEY (visit_id, purpose_id),
    CONSTRAINT fk_vp_visit   FOREIGN KEY (visit_id)   REFERENCES visits(id)   ON DELETE CASCADE,
    CONSTRAINT fk_vp_purpose FOREIGN KEY (purpose_id) REFERENCES purposes(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================================
--  D BÖLMƏ — GİRİŞ-ÇIXIŞ HADİSƏLƏRİ VƏ SİSTEM LOQLARI
-- =====================================================================

-- Giriş-çıxış hadisələri (qapıda hər skan / manual təsdiq)
-- "Giriş-çıxış tarixçəsi" səhifəsi buradan və visits-dən qurulur.
CREATE TABLE access_events (
    id           BIGINT   NOT NULL AUTO_INCREMENT,
    visit_id     BIGINT   NOT NULL,
    event_type   ENUM('entry','exit')       NOT NULL,   -- giriş / çıxış
    method       ENUM('card','qr','manual') NOT NULL,   -- kartla / QR ilə / əl ilə (mühafizə)
    area_id      BIGINT       NULL,                      -- hansı ərazidə skan olundu
    recorded_by  BIGINT       NULL,                      -- manual olduqda təsdiqləyən istifadəçi
    occurred_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_ae_visit (visit_id),
    KEY ix_ae_time (occurred_at),
    CONSTRAINT fk_ae_visit FOREIGN KEY (visit_id)    REFERENCES visits(id) ON DELETE CASCADE,
    CONSTRAINT fk_ae_area  FOREIGN KEY (area_id)     REFERENCES areas(id)  ON DELETE SET NULL,
    CONSTRAINT fk_ae_user  FOREIGN KEY (recorded_by) REFERENCES users(id)  ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Sistem loqları (audit: kim, nə vaxt, hansı əməliyyatı etdi)
CREATE TABLE system_logs (
    id           BIGINT       NOT NULL AUTO_INCREMENT,
    user_id      BIGINT           NULL,               -- əməliyyatı edən istifadəçi
    action       VARCHAR(80)  NOT NULL,               -- 'login','logout','guest.create','visit.checkout','card.deactivate' ...
    entity_type  VARCHAR(60)      NULL,               -- 'guest','visit','card','user','role' ...
    entity_id    BIGINT           NULL,
    description  VARCHAR(500)     NULL,
    ip_address   VARCHAR(45)      NULL,               -- IPv4/IPv6
    created_at   TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_logs_user (user_id),
    KEY ix_logs_action (action),
    KEY ix_logs_time (created_at),
    CONSTRAINT fk_logs_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- (Opsional) Autentifikasiya sessiyaları / refresh token-lər
CREATE TABLE auth_sessions (
    id           BIGINT       NOT NULL AUTO_INCREMENT,
    user_id      BIGINT       NOT NULL,
    token_hash   VARCHAR(255) NOT NULL,
    ip_address   VARCHAR(45)      NULL,
    user_agent   VARCHAR(255)     NULL,
    expires_at   DATETIME     NOT NULL,
    created_at   TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY ix_sess_user (user_id),
    CONSTRAINT fk_sess_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================================
--  BAŞLANĞIC (SEED) MƏLUMATLARI — istinad üçün
-- =====================================================================

-- Bölmələr (sidebar-a uyğun)
INSERT INTO sections (code, name, sort_order) VALUES
  ('dashboard',      'Ana səhifə',            1),
  ('guests',         'Qonaqlar',              2),
  ('cards',          'Kartlar',               3),
  ('history',        'Giriş-çıxış tarixçəsi', 4),
  ('active_permits', 'Aktiv icazələr',        5),
  ('reports',        'Hesabatlar',            6),
  ('logs',           'Sistem Loqları',        7),
  ('users',          'İstifadəçilər',         8),
  ('roles',          'Rollar',                9),
  ('settings',       'Parametrlər',          10);

-- Əsas rollar
INSERT INTO roles (name, description, is_system) VALUES
  ('Administrator', 'Sistem idarəetməsi',  1),
  ('Mühafizə',      'Giriş-çıxış nəzarəti', 0),
  ('Qəbul',         'Qonaq qəbulu',         0);

-- Qeyd: role_permissions Administrator üçün bütün bölmələrdə
-- (can_view, can_add, can_edit, can_delete) = 1 olaraq doldurulur;
-- Mühafizə və Qəbul üçün isə tətbiqdəki matris əsasında.
