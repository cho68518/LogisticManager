DELIMITER $$

DROP PROCEDURE IF EXISTS sp_Table_Cre$$

CREATE PROCEDURE sp_Table_Cre()
BEGIN
    -- 'gramwonlogis2' �����ͺ��̽��� ���� ���� ���̺� ������ ����
    TRUNCATE TABLE gramwonlogis2.�������_���ݿ�����ȯ;
    TRUNCATE TABLE gramwonlogis2.�������_�޼���;
    TRUNCATE TABLE gramwonlogis2.�������_�޼����λ�;
    TRUNCATE TABLE gramwonlogis2.�������_�޼���û��;
    TRUNCATE TABLE gramwonlogis2.�������_Ư�����_�����庯��;
    TRUNCATE TABLE gramwonlogis2.�������_Ư�����_��õ�и����;
    TRUNCATE TABLE gramwonlogis2.�������_�ֹ�����;
    TRUNCATE TABLE gramwonlogis2.�������_����Ұ�;
    TRUNCATE TABLE gramwonlogis2.�������_������;
    TRUNCATE TABLE gramwonlogis2.�������_������_��۸޼���;
    TRUNCATE TABLE gramwonlogis2.�������_������_�̸�;
	TRUNCATE TABLE gramwonlogis2.��ǥ����;
	TRUNCATE TABLE gramwonlogis2.�������_����ڽ�;

END $$

DELIMITER ;






CALL sp_Table_Cre();


