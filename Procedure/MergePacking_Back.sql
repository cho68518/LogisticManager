DELIMITER $$

CREATE PROCEDURE MergePacking1()
BEGIN
    -- ���� ����
    DECLARE v_emp_id INT;
    DECLARE v_emp_name VARCHAR(100);
    DECLARE v_salary DECIMAL(10,2);

	DECLARE v_receiver_name         VARCHAR(255); -- �����θ�
	DECLARE v_phone1                VARCHAR(255); -- ��ȭ��ȣ1
	DECLARE v_phone2                VARCHAR(255); -- ��ȭ��ȣ2
	DECLARE v_zipcode               VARCHAR(255); -- �����ȣ
	DECLARE v_address               VARCHAR(255); -- �ּ�
	DECLARE v_option_name           VARCHAR(255); -- �ɼǸ�
	DECLARE v_quantity              INT(11);      -- ����
	DECLARE v_delivery_msg          VARCHAR(255); -- ��۸޼���
	DECLARE v_order_no              VARCHAR(255); -- �ֹ���ȣ
	DECLARE v_shop_name             VARCHAR(255); -- ���θ�
	DECLARE v_collected_at          DATETIME;     -- �����ð�
	DECLARE v_invoice_name          VARCHAR(255); -- �����
	DECLARE v_item_code             VARCHAR(255); -- ǰ���ڵ�
	DECLARE v_shipping_cost         VARCHAR(255); -- �ù���
	DECLARE v_box_size              VARCHAR(255); -- �ڽ�ũ��
	DECLARE v_print_count           VARCHAR(255); -- ��°���
	DECLARE v_invoice_qty           VARCHAR(255); -- �������
	DECLARE v_star1                 VARCHAR(255); -- ��ǥ1
	DECLARE v_star2                 VARCHAR(255); -- ��ǥ2
	DECLARE v_item_count            VARCHAR(255); -- ǰ�񰳼�
	DECLARE v_shipping_qty          VARCHAR(255); -- �ù����
	DECLARE v_shipping_qty1         VARCHAR(255); -- �ù����1
	DECLARE v_shipping_qty_total    VARCHAR(255); -- �ù�����ջ�
	DECLARE v_invoice_divider       VARCHAR(255); -- ���屸����
	DECLARE v_invoice_div           VARCHAR(256); -- ���屸��
	DECLARE v_invoice_div_final     VARCHAR(256); -- ���屸������
	DECLARE v_location              VARCHAR(255); -- ��ġ
	DECLARE v_location_conv         VARCHAR(255); -- ��ġ��ȯ
	DECLARE v_order_no_shop         VARCHAR(255); -- �ֹ���ȣ(���θ�)
	DECLARE v_payment_amount        VARCHAR(255); -- �����ݾ�
	DECLARE v_order_amount          VARCHAR(255); -- �ֹ��ݾ�
	DECLARE v_payment_method        VARCHAR(255); -- ��������
	DECLARE v_tax_type              VARCHAR(255); -- ���������
	DECLARE v_order_status          VARCHAR(255); -- �ֹ�����
	DECLARE v_shipping_send         VARCHAR(255); -- ��ۼ�

    DECLARE v_item_name             VARCHAR(255); -- ǰ���
	DECLARE v_alt_code1             VARCHAR(255); -- ��ü�ڵ�1
	DECLARE v_alt_code1_name        VARCHAR(255); -- ��ü�ڵ�1ǰ���
	DECLARE v_alt1_qty              VARCHAR(255); -- ��ü1����
	DECLARE v_alt_code2             VARCHAR(255); -- ��ü�ڵ�2
	DECLARE v_alt_code2_name        VARCHAR(255); -- ��ü�ڵ�2ǰ���
	DECLARE v_alt2_qty              VARCHAR(255); -- ��ü2����
	DECLARE v_alt_code3             VARCHAR(255); -- ��ü�ڵ�3
	DECLARE v_alt_code3_name        VARCHAR(255); -- ��ü�ڵ�3ǰ���
	DECLARE v_alt3_qty              VARCHAR(255); -- ��ü3����
	DECLARE v_alt_code4             VARCHAR(255); -- ��ü�ڵ�4
	DECLARE v_alt_code4_name        VARCHAR(255); -- ��ü�ڵ�4ǰ���
	DECLARE v_alt4_qty              VARCHAR(255); -- ��ü4����

    -- Ŀ�� ���� ������ ����
    DECLARE done INT DEFAULT 0;

    -- Ŀ�� ���� (���� ���� ��ü employees ���̺� �б�)
	DECLARE cur1 CURSOR FOR	
		SELECT msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, �ɼǸ�,
			   ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���, �ڽ�ũ��, ��°���, �������,
		   	   ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�, ���屸����, ���屸��, ���屸������, ��ġ,
			   ��ġ��ȯ, `�ֹ���ȣ(���θ�)`, �����ݾ�, �ֹ��ݾ�, ��������, ���������, �ֹ�����, ��ۼ�
	     FROM �������_���ݿ�����ȯ_�����庯ȯ;

	DECLARE cur2 CURSOR FOR	
		SELECT ǰ���ڵ�, ǰ���, 
		       ��ü�ڵ�1, ��ü�ڵ�1ǰ���, ��ü1����,
		   	   ��ü�ڵ�2, ��ü�ڵ�2ǰ���, ��ü2����,
			   ��ü�ڵ�3, ��ü�ڵ�3ǰ���, ��ü3����,
			   ��ü�ڵ�4, ��ü�ڵ�4ǰ���, ��ü4����
	     FROM �������_Ư�����_�����庯��;
		 
    -- Ŀ�� ���� ���� �ڵ鷯
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

    ---------------------------------------------------------------------
	-- ��ó��
    ---------------------------------------------------------------------
	-- ������ ���� �ֹ������Ǹ� ����
	TRUNCATE TABLE �������_���ݿ�����ȯ_�����庯ȯ;
	
    INSERT INTO �������_���ݿ�����ȯ_�����庯ȯ
    SELECT * FROM �������_���ݿ�����ȯ_Dev
    WHERE ǰ���ڵ� IN (SELECT ǰ���ڵ� FROM �������_Ư�����_�����庯��);

    -------------------------------------------------------------------------------------------------------
    -- ������ ���� Ư��ó�� �� ����
    -------------------------------------------------------------------------------------------------------	
    -- ������ ��� �ֹ����� ���� (������ ó���� ��üǰ������� �ֹ��� Insert)
    DELETE FROM �������_���ݿ�����ȯ_Dev
    WHERE ǰ���ڵ� IN (SELECT ǰ���ڵ� FROM �������_Ư�����_�����庯��);

    -- �����庯ȯ1 (��ü�ڵ�2 �ֹ������Ǹ� ����)
	TRUNCATE TABLE �������_���ݿ�����ȯ_�����庯ȯ1;
	
    INSERT INTO �������_���ݿ�����ȯ_�����庯ȯ1
    SELECT a.*
      FROM �������_���ݿ�����ȯ_�����庯ȯ a
      JOIN �������_Ư�����_�����庯�� b ON a.ǰ���ڵ� = b.ǰ���ڵ�
     WHERE b.��ü�ڵ�2 != '';

    -- �����庯ȯ2 (��ü�ڵ�3 �ֹ������Ǹ� ����)
	TRUNCATE TABLE �������_���ݿ�����ȯ_�����庯ȯ2;
	
    INSERT INTO �������_���ݿ�����ȯ_�����庯ȯ2
    SELECT a.*
      FROM �������_���ݿ�����ȯ_�����庯ȯ a
      JOIN �������_Ư�����_�����庯�� b ON a.ǰ���ڵ� = b.ǰ���ڵ�
     WHERE b.��ü�ڵ�3 != '';

    -- �����庯ȯ3 (��ü�ڵ�4 �ֹ������Ǹ� ����)	
	TRUNCATE TABLE �������_���ݿ�����ȯ_�����庯ȯ3;	
	
    INSERT INTO �������_���ݿ�����ȯ_�����庯ȯ3
    SELECT a.*
      FROM �������_���ݿ�����ȯ_�����庯ȯ a
      JOIN �������_Ư�����_�����庯�� b ON a.ǰ���ڵ� = b.ǰ���ڵ�
     WHERE b.��ü�ڵ�4 != '';		 

    -- �����庯ȯ 11		 
    UPDATE �������_���ݿ�����ȯ_�����庯ȯ a
      JOIN �������_Ư�����_�����庯�� b ON a.ǰ���ڵ� = b.ǰ���ڵ�
       SET 
           a.�ù��� = b.��ü�ڵ�1,
           a.�ڽ�ũ�� = b.��ü�ڵ�1ǰ���,
           a.��°��� = b.��ü1����
     WHERE b.��ü�ڵ�1 != '';		 

   -- �����庯ȯ 22		 
   UPDATE �������_���ݿ�����ȯ_�����庯ȯ1 a
     JOIN �������_Ư�����_�����庯�� b ON a.ǰ���ڵ� = b.ǰ���ڵ�
      SET 
          a.�ù��� = b.��ü�ڵ�2,
          a.�ڽ�ũ�� = b.��ü�ڵ�2ǰ���,
          a.��°��� = b.��ü2����
    WHERE b.��ü�ڵ�2 != '';
		 
   -- �����庯ȯ 33
   UPDATE �������_���ݿ�����ȯ_�����庯ȯ2 a
     JOIN �������_Ư�����_�����庯�� b ON a.ǰ���ڵ� = b.ǰ���ڵ�
      SET 
          a.�ù��� = b.��ü�ڵ�3,
          a.�ڽ�ũ�� = b.��ü�ڵ�3ǰ���,
          a.��°��� = b.��ü3����
    WHERE b.��ü�ڵ�3 != '';
		 
   -- �����庯ȯ 44
   UPDATE �������_���ݿ�����ȯ_�����庯ȯ3 a
     JOIN �������_Ư�����_�����庯�� b ON a.ǰ���ڵ� = b.ǰ���ڵ�
      SET 
          a.�ù��� = b.��ü�ڵ�4,
          a.�ڽ�ũ�� = b.��ü�ڵ�4ǰ���,
          a.��°��� = b.��ü4����
    WHERE b.��ü�ڵ�4 != '';

    -- �ڽ�ũ�Ⱑ ����ִ� �� ����
	DELETE FROM �������_���ݿ�����ȯ_�����庯ȯ1
     WHERE �ڽ�ũ�� IS NULL OR �ڽ�ũ�� = '';
		 
    DELETE FROM �������_���ݿ�����ȯ_�����庯ȯ2
     WHERE �ڽ�ũ�� IS NULL OR �ڽ�ũ�� = '';
		 
    DELETE FROM �������_���ݿ�����ȯ_�����庯ȯ3
     WHERE �ڽ�ũ�� IS NULL OR �ڽ�ũ�� = '';
		 
    UPDATE �������_���ݿ�����ȯ_�����庯ȯ
       SET 
           ǰ���ڵ� = �ù���,
           �����   = �ڽ�ũ��,
           ����     = ���� * ��°���,
           �ù��� = NULL,
           �ڽ�ũ�� = NULL,
           ��°��� = NULL;

    UPDATE �������_���ݿ�����ȯ_�����庯ȯ1
       SET 
           ǰ���ڵ� = �ù���,
           �����   = �ڽ�ũ��,
           ����     = ���� * ��°���,
           �ù��� = NULL,
           �ڽ�ũ�� = NULL,
           ��°��� = NULL;
		   
    UPDATE �������_���ݿ�����ȯ_�����庯ȯ2
       SET 
           ǰ���ڵ� = �ù���,
           �����   = �ڽ�ũ��,
           ����     = ���� * ��°���,
           �ù��� = NULL,
           �ڽ�ũ�� = NULL,
           ��°��� = NULL;
		   
    UPDATE �������_���ݿ�����ȯ_�����庯ȯ3
       SET 
           ǰ���ڵ� = �ù���,
           �����   = �ڽ�ũ��,
           ����     = ���� * ��°���,
           �ù��� = NULL,
           �ڽ�ũ�� = NULL,
           ��°��� = NULL;	
		   
    UPDATE �������_���ݿ�����ȯ_�����庯ȯ1 s
        JOIN ǰ���� p ON s.ǰ���ڵ� = p.ǰ���ڵ�
        SET s.�����ݾ� = 0,
            s.�ֹ��ݾ� = 0;
			
    UPDATE �������_���ݿ�����ȯ_�����庯ȯ2 s
        JOIN ǰ���� p ON s.ǰ���ڵ� = p.ǰ���ڵ�
        SET s.�����ݾ� = 0,
            s.�ֹ��ݾ� = 0;
			
    UPDATE �������_���ݿ�����ȯ_�����庯ȯ3 s
        JOIN ǰ���� p ON s.ǰ���ڵ� = p.ǰ���ڵ�
        SET s.�����ݾ� = 0,
            s.�ֹ��ݾ� = 0;			
		   
		   
    -- ������ ���� �� ���� ����		   
	TRUNCATE TABLE �������_���ݿ�����ȯ_�����庯ȯ���;		   
		   
    INSERT INTO �������_���ݿ�����ȯ_�����庯ȯ��� (
        msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, 
        �ɼǸ�, ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���, 
        �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�, 
        ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ, `�ֹ���ȣ(���θ�)`, �����ݾ�, �ֹ��ݾ�, ��������, ���������, �ֹ�����, ��ۼ�
    )
    SELECT 
        msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, 
        �ɼǸ�, ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���, 
        �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�, 
        ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ, `�ֹ���ȣ(���θ�)`, �����ݾ�, �ֹ��ݾ�, ��������, ���������, �ֹ�����, ��ۼ�
    FROM �������_���ݿ�����ȯ_�����庯ȯ
    UNION ALL
    SELECT 
        msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, 
        �ɼǸ�, ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���, 
        �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�, 
        ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ, `�ֹ���ȣ(���θ�)`, �����ݾ�, �ֹ��ݾ�, ��������, ���������, �ֹ�����, ��ۼ�
    FROM �������_���ݿ�����ȯ_�����庯ȯ1
    UNION ALL
    SELECT 
        msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, 
        �ɼǸ�, ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���, 
        �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�, 
        ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ, `�ֹ���ȣ(���θ�)`, �����ݾ�, �ֹ��ݾ�, ��������, ���������, �ֹ�����, ��ۼ�
    FROM �������_���ݿ�����ȯ_�����庯ȯ2
    UNION ALL
    SELECT 
        msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, 
        �ɼǸ�, ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���, 
        �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�, 
        ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ, `�ֹ���ȣ(���θ�)`, �����ݾ�, �ֹ��ݾ�, ��������, ���������, �ֹ�����, ��ۼ�
    FROM �������_���ݿ�����ȯ_�����庯ȯ3;

    -------------------------------------------------------------------------------------------------------
    -- �������_���ݿ�����ȯ(�ֹ����� �������̺�) ���̺� ������ �߰�
    -------------------------------------------------------------------------------------------------------	
   INSERT INTO �������_���ݿ�����ȯ (
       msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, 
       �ɼǸ�, ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���, 
       �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�, 
       ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ, `�ֹ���ȣ(���θ�)`, �����ݾ�, �ֹ��ݾ�, ��������, ���������, �ֹ�����, ��ۼ�
   )
   SELECT 
       msg1, msg2, msg3, msg4, msg5, msg6, �����θ�, ��ȭ��ȣ1, ��ȭ��ȣ2, �����ȣ, �ּ�, 
       �ɼǸ�, ����, ��۸޼���, �ֹ���ȣ, ���θ�, �����ð�, �����, ǰ���ڵ�, �ù���, 
       �ڽ�ũ��, ��°���, �������, ��ǥ1, ��ǥ2, ǰ�񰳼�, �ù����, �ù����1, �ù�����ջ�, 
       ���屸����, ���屸��, ���屸������, ��ġ, ��ġ��ȯ, `�ֹ���ȣ(���θ�)`, �����ݾ�, �ֹ��ݾ�, ��������, ���������, �ֹ�����, ��ۼ�
   FROM �������_���ݿ�����ȯ_�����庯ȯ���;







    ---------------------------------------------------------------------
    -- Ŀ�� ����
    ---------------------------------------------------------------------	
    /*OPEN cur2;

    read_loop: LOOP
        -- Ŀ������ �� �� �б�
        --FETCH cur1 INTO v_receiver_name, v_phone1, v_phone2, v_zipcode, v_address, v_option_name,
        --                v_quantity, v_delivery_msg, v_order_no, v_shop_name, v_collected_at,
        --                v_invoice_name, v_item_code, v_shipping_cost, v_box_size, v_print_count,
        --                v_invoice_qty, v_star1, v_star2, v_item_count, v_shipping_qty, v_shipping_qty1,
        --                v_shipping_qty_total, v_invoice_divider, v_invoice_div, v_invoice_div_final,
        --                v_location, v_location_conv, v_order_no_shop, v_payment_amount, v_order_amount,
        --                v_payment_method, v_tax_type, v_order_status, v_shipping_send;

        FETCH cur2 INTO v_item_code, v_item_name, 
		                v_alt_code1, v_alt_code1_name, v_alt1_qty,
		                v_alt_code1, v_alt_code1_name, v_alt1_qty,
		                v_alt_code1, v_alt_code1_name, v_alt1_qty,
		                v_alt_code1, v_alt_code1_name, v_alt1_qty;


        -- �� �̻� ���� �����Ͱ� ������ ���� ����
        IF done = 1 THEN
            LEAVE read_loop;
        END IF;






        -- ���� ó��: ������ 5000 �̻��� ��츸 �ٸ� ���̺� ����
        --IF v_salary >= 5000 THEN
        --    INSERT INTO high_salary_employees(emp_id, emp_name, salary, processed_at)
        --    VALUES (v_emp_id, v_emp_name, v_salary, NOW());
        --END IF;

		SELECT ��ü�ڵ�1, ��ü�ڵ�1ǰ���, ��ü1����,
		       ��ü�ڵ�2, ��ü�ڵ�2ǰ���, ��ü2����,
			   ��ü�ڵ�3, ��ü�ڵ�3ǰ���, ��ü3����,
			   ��ü�ڵ�4, ��ü�ڵ�4ǰ���, ��ü4����
	      INTO v_alt_code1, v_alt_code1_name, v_alt_code1_qty,
		       v_alt_code2, v_alt_code2_name, v_alt_code2_qty,
			   v_alt_code3, v_alt_code3_name, v_alt_code3_qty,
			   v_alt_code4, v_alt_code4_name, v_alt_code4_qty
		  FROM �������_Ư�����_�����庯��
		 WHERE ;
		
		UPDATE �������_���ݿ�����ȯ_�����庯ȯ
          JOIN �������_Ư�����_�����庯�� b ON a.ǰ���ڵ� = b.ǰ���ڵ�
           SET 
               a.�ù��� = b.��ü�ڵ�1,
               a.�ڽ�ũ�� = b.��ü�ڵ�1ǰ���,
               a.��°��� = b.��ü1����
         WHERE ǰ���ڵ� = v_item_code     v_item_code = b.��ü�ڵ�1 != ''
		
		
		
		
		
		
		
		
		
    END LOOP;

    ---------------------------------------------------------------------
    CLOSE cur2;
    ---------------------------------------------------------------------
	
	*/
	
	
	
END$$

DELIMITER ;