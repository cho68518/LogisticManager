DELIMITER $$

CREATE PROCEDURE MergePacking()
BEGIN
    -- ���� �߻� �� �ѹ� �� �� ���� �޽��� ����� ���� �ڵ鷯 ����
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        -- ���� ������ ���� ���� ����
        DECLARE v_sqlstate CHAR(5);
        DECLARE v_error_no INT;
        DECLARE v_error_message TEXT;

        -- �߻��� ������ �� ������ �����ɴϴ�.
        GET DIAGNOSTICS CONDITION 1
            v_sqlstate = RETURNED_SQLSTATE,
            v_error_no = MYSQL_ERRNO,
            v_error_message = MESSAGE_TEXT;

        -- Ʈ������� �ѹ��մϴ�.
        ROLLBACK;

        -- �� ���� ������ ������ �޽����� ����մϴ�.
        SELECT CONCAT(
            'Error (SQLSTATE: ', v_sqlstate, ', Error No: ', v_error_no, '): ', v_error_message, 
            ' | The transaction was rolled back.'
        ) AS ErrorMessage;
    END;

    -- Ʈ����� ����
    START TRANSACTION;

    -- ó�� ��� ���� �ֹ� �����͸� CTE�� ����
 WITH PackingSource AS (
    -- dev.* ��� �ʿ��� ��� �÷��� ��������� ����.
    SELECT 
        dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, 
        dev.�����θ�, dev.��ȭ��ȣ1, dev.��ȭ��ȣ2, dev.�����ȣ, dev.�ּ�,
        dev.�ɼǸ�, dev.����, dev.��۸޼���, dev.�ֹ���ȣ, dev.���θ�, dev.�����ð�,
        dev.�������, dev.��ǥ1, dev.��ǥ2, dev.ǰ�񰳼�, dev.�ù����, dev.�ù����1, dev.�ù�����ջ�,
        dev.���屸����, dev.���屸��, dev.���屸������, dev.��ġ, dev.��ġ��ȯ, dev.`�ֹ���ȣ(���θ�)`,
        dev.�����ݾ�, dev.�ֹ��ݾ�, dev.��������, dev.���������, dev.�ֹ�����, dev.��ۼ�,
        -- spec ���̺����� ��ü�ڵ� ���� �÷��� �����ɴϴ�.
        spec.��ü�ڵ�1, spec.��ü�ڵ�1ǰ���, spec.��ü1����,
        spec.��ü�ڵ�2, spec.��ü�ڵ�2ǰ���, spec.��ü2����,
        spec.��ü�ڵ�3, spec.��ü�ڵ�3ǰ���, spec.��ü3����,
        spec.��ü�ڵ�4, spec.��ü�ڵ�4ǰ���, spec.��ü4����
    FROM �������_���ݿ�����ȯ_Dev dev
    JOIN �������_Ư�����_�����庯�� spec ON dev.ǰ���ڵ� = spec.ǰ���ڵ�
)
-- ������ ��ȯ �����͸� ���� ���̺� INSERT
-- CTE(WITH)�� ����ϴ� ���, FROM ���� ���� ���������� ����մϴ�.
INSERT INTO �������_���ݿ�����ȯ_Dev (
    msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�,
    �ɼǸ�, ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, 
    �����, ǰ���ڵ�, �ù���, �ڽ�ũ��, ��°���, 
    �������, ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�,
    ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ, `�ֹ���ȣ(���θ�)`, �����ݾ�, �ֹ��ݾ�, 
    ��������, ���������, �ֹ�����, ��ۼ�
)
SELECT
    sub.msg1, sub.msg2, sub.msg3, sub.msg4, sub.msg5, sub.msg6, sub.�����θ�, sub.��ȭ��ȣ1, sub.��ȭ��ȣ2, sub.�����ȣ, sub.�ּ�,
    sub.�ɼǸ�, (sub.���� * sub.��ü1����), sub.��۸޼���, sub.�ֹ���ȣ, sub.���θ�, sub.�����ð�, 
    sub.��ü�ڵ�1ǰ���, sub.��ü�ڵ�1, 
    NULL, NULL, NULL,
    sub.�������, sub.��ǥ1, sub.��ǥ2, sub.ǰ�񰳼�, sub.�ù����, sub.�ù����1, sub.�ù�����ջ�,
    sub.���屸����, sub.���屸��, sub.���屸������, sub.��ġ, sub.��ġ��ȯ, sub.`�ֹ���ȣ(���θ�)`, sub.�����ݾ�, sub.�ֹ��ݾ�, 
    sub.��������, sub.���������, sub.�ֹ�����, sub.��ۼ�
FROM (
    -- �� �κ��� ������ CTE(PackingSource)�� ������ ������ �մϴ�.
    SELECT 
        dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, 
        dev.�����θ�, dev.��ȭ��ȣ1, dev.��ȭ��ȣ2, dev.�����ȣ, dev.�ּ�,
        dev.�ɼǸ�, dev.����, dev.��۸޼���, dev.�ֹ���ȣ, dev.���θ�, dev.�����ð�,
        dev.�������, dev.��ǥ1, dev.��ǥ2, dev.ǰ�񰳼�, dev.�ù����, dev.�ù����1, dev.�ù�����ջ�,
        dev.���屸����, dev.���屸��, dev.���屸������, dev.��ġ, dev.��ġ��ȯ, dev.`�ֹ���ȣ(���θ�)`,
        dev.�����ݾ�, dev.�ֹ��ݾ�, dev.��������, dev.���������, dev.�ֹ�����, dev.��ۼ�,
        spec.��ü�ڵ�1, spec.��ü�ڵ�1ǰ���, spec.��ü1����,
        spec.��ü�ڵ�2, spec.��ü�ڵ�2ǰ���, spec.��ü2����,
        spec.��ü�ڵ�3, spec.��ü�ڵ�3ǰ���, spec.��ü3����,
        spec.��ü�ڵ�4, spec.��ü�ڵ�4ǰ���, spec.��ü4����
    FROM �������_���ݿ�����ȯ_Dev dev
    JOIN �������_Ư�����_�����庯�� spec ON dev.ǰ���ڵ� = spec.ǰ���ڵ�
) AS sub -- ������������ �ݵ�� ��Ī(AS)�� �ʿ��մϴ�.
WHERE sub.��ü�ڵ�1 IS NOT NULL AND sub.��ü�ڵ�1 != ''
UNION ALL
-- (���� UNION ALL �����鵵 ��� sub.�÷��� ���·� ����)
SELECT
    sub.msg1, sub.msg2, sub.msg3, sub.msg4, sub.msg5, sub.msg6, sub.�����θ�, sub.��ȭ��ȣ1, sub.��ȭ��ȣ2, sub.�����ȣ, sub.�ּ�,
    sub.�ɼǸ�, (sub.���� * sub.��ü2����), sub.��۸޼���, sub.�ֹ���ȣ, sub.���θ�, sub.�����ð�, 
    sub.��ü�ڵ�2ǰ���, sub.��ü�ڵ�2, 
    NULL, NULL, NULL,
    sub.�������, sub.��ǥ1, sub.��ǥ2, sub.ǰ�񰳼�, sub.�ù����, sub.�ù����1, sub.�ù�����ջ�,
    sub.���屸����, sub.���屸��, sub.���屸������, sub.��ġ, sub.��ġ��ȯ, sub.`�ֹ���ȣ(���θ�)`, 0, 0,
    sub.��������, sub.���������, sub.�ֹ�����, sub.��ۼ�
FROM (SELECT dev.msg1, dev.msg2, dev.msg3, dev.msg4, dev.msg5, dev.msg6, dev.�����θ�, dev.��ȭ��ȣ1, dev.��ȭ��ȣ2, dev.�����ȣ, dev.�ּ�, dev.�ɼǸ�, dev.����, dev.��۸޼���, dev.�ֹ���ȣ, dev.���θ�, dev.�����ð�, dev.�������, dev.��ǥ1, dev.��ǥ2, dev.ǰ�񰳼�, dev.�ù����, dev.�ù����1, dev.�ù�����ջ�, dev.���屸����, dev.���屸��, dev.���屸������, dev.��ġ, dev.��ġ��ȯ, dev.`�ֹ���ȣ(���θ�)`, dev.�����ݾ�, dev.�ֹ��ݾ�, dev.��������, dev.���������, dev.�ֹ�����, dev.��ۼ�, spec.��ü�ڵ�1, spec.��ü�ڵ�1ǰ���, spec.��ü1����, spec.��ü�ڵ�2, spec.��ü�ڵ�2ǰ���, spec.��ü2����, spec.��ü�ڵ�3, spec.��ü�ڵ�3ǰ���, spec.��ü3����, spec.��ü�ڵ�4, spec.��ü�ڵ�4ǰ���, spec.��ü4���� FROM �������_���ݿ�����ȯ_Dev dev JOIN �������_Ư�����_�����庯�� spec ON dev.ǰ���ڵ� = spec.ǰ���ڵ�) AS sub
WHERE sub.��ü�ڵ�2 IS NOT NULL AND sub.��ü�ڵ�2 != ''
-- (���� UNION ALL 3, 4�� ������ �������� ����)
;

    -- ���� �ֹ� ������ ����
    DELETE FROM �������_���ݿ�����ȯ_Dev
    WHERE ǰ���ڵ� IN (SELECT ǰ���ڵ� FROM �������_Ư�����_�����庯��);
    
    -- �۾� ���� �� ���� ����
    COMMIT;
	
	

END$$

DELIMITER ;