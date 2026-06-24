DO $$
DECLARE
    v_menu_id integer;
    v_sequence_name text;
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM public."MenuTanimlari"
        WHERE "Kod" = 'saha-gerceklesen-ceki-raporu'
    ) THEN
        IF NOT EXISTS (
            SELECT 1
            FROM public."MenuTanimlari"
            WHERE "Id" = 42
        ) THEN
            INSERT INTO public."MenuTanimlari"
                ("Id", "Kod", "LabelKey", "Icon", "Route", "Sira", "ParentId", "CreatedDate")
            VALUES
                (42, 'saha-gerceklesen-ceki-raporu', 'MENU.SAHA_GERCEKLESEN_CEKI_RAPORU', '', NULL, 9, 17, NOW());
        ELSE
            INSERT INTO public."MenuTanimlari"
                ("Kod", "LabelKey", "Icon", "Route", "Sira", "ParentId", "CreatedDate")
            VALUES
                ('saha-gerceklesen-ceki-raporu', 'MENU.SAHA_GERCEKLESEN_CEKI_RAPORU', '', NULL, 9, 17, NOW());
        END IF;
    END IF;

    SELECT "Id"
    INTO v_menu_id
    FROM public."MenuTanimlari"
    WHERE "Kod" = 'saha-gerceklesen-ceki-raporu';

    IF v_menu_id IS NOT NULL AND NOT EXISTS (
        SELECT 1
        FROM public."RolYetkileri"
        WHERE "RolId" = 1 AND "MenuTanimiId" = v_menu_id
    ) THEN
        INSERT INTO public."RolYetkileri"
            ("RolId", "MenuTanimiId", "YetkiTipiId", "CreatedDate")
        VALUES
            (1, v_menu_id, 3, NOW());
    END IF;

    v_sequence_name := pg_get_serial_sequence('public."MenuTanimlari"', 'Id');
    IF v_sequence_name IS NOT NULL THEN
        EXECUTE format(
            'SELECT setval(%L, (SELECT GREATEST(COALESCE(MAX("Id"), 1), 1) FROM public."MenuTanimlari"), true)',
            v_sequence_name
        );
    END IF;
END $$;
