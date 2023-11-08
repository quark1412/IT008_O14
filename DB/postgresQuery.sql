CREATE TABLE Account (
    username VARCHAR(100) PRIMARY KEY,
    password VARCHAR(1000) NOT NULL,
    role VARCHAR(100) DEFAULT 'student'
);

-- Tạo bảng "Profile"
CREATE TABLE Profile (
    id VARCHAR(100) PRIMARY KEY,
    name VARCHAR(100),
    birthday DATE,
    gender VARCHAR(100),
    level VARCHAR(100) DEFAULT 'Đại học',
    trainingSystem VARCHAR(100) DEFAULT 'Chính quy',
    avatar BYTEA  -- Sử dụng kiểu BYTEA cho dữ liệu hình ảnh
);

-- Tạo bảng "UserAcc"
CREATE TABLE UserAcc (
    id SERIAL PRIMARY KEY,  -- Sử dụng SERIAL cho cột id để tự động tạo giá trị duy nhất
    idAccount VARCHAR(100) REFERENCES Account(username),
    idProfile VARCHAR(100) REFERENCES Profile(id)
);

-- Tạo bảng "Course"
CREATE TABLE Course (
    id VARCHAR(100) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    numberOfCredits INT NOT NULL,
    schoolDay VARCHAR(100) NOT NULL,
    lesson VARCHAR(100) NOT NULL,
    classroom VARCHAR(100) NOT NULL,
    semester VARCHAR(100) NOT NULL,
    schoolYear VARCHAR(100) NOT NULL,
    startDay DATE NOT NULL,
    endDay DATE NOT NULL
);

-- Tạo bảng "Score"
CREATE TABLE Score (
    id SERIAL PRIMARY KEY,  -- Sử dụng SERIAL cho cột id để tự động tạo giá trị duy nhất
    processScore FLOAT,
    midtermScore FLOAT,
    finalScore FLOAT,
    practiceScore FLOAT,
    ratioProcess FLOAT DEFAULT 0.1,
    ratioMidterm FLOAT DEFAULT 0.2,
    ratioPractice FLOAT DEFAULT 0.3,
    ratioFinal FLOAT DEFAULT 0.4
);

-- Tạo bảng "Schedule"
CREATE TABLE Schedule (
    id SERIAL PRIMARY KEY,  -- Sử dụng SERIAL cho cột id để tự động tạo giá trị duy nhất
    idProfile VARCHAR(100) NOT NULL REFERENCES Profile (id),
    idCourse VARCHAR(100) NOT NULL REFERENCES Course (id),
    idScore INT REFERENCES Score (id),
    note VARCHAR(1000)
);

-- Store procedure 
CREATE OR REPLACE FUNCTION "Login"(_username VARCHAR(100), _password VARCHAR(1000))
RETURNS TABLE("id" VARCHAR(100), "role" VARCHAR(100)) AS $$
BEGIN
    RETURN QUERY 
    SELECT Profile.id as "MSSV" , Account.role as "Role"
    FROM Account, Profile, UserAcc
    WHERE   Account.username = UserAcc.idAccount AND
            UserAcc.idProfile = Profile.id AND
            username = _username and password = _password;
END;
$$ LANGUAGE plpgsql;

-- SELECT * FROM "Login"('student1', '123456');

CREATE OR REPLACE FUNCTION LoadProfileById(_id VARCHAR(100))
RETURNS TABLE("MSSV" VARCHAR(100), "Tên" VARCHAR(100), "Ngày sinh" DATE, "Giới tính" VARCHAR(100), "Bậc đào tạo" VARCHAR(100), "Hệ đào tạo" VARCHAR(100), "Ảnh đại diện" BYTEA) AS $$
BEGIN
    RETURN QUERY
    SELECT id as "MSSV",
           name as "Tên",
           birthday as "Ngày sinh",
           gender as "Giới tính",
           level as "Bậc đào tạo",
           trainingSystem as "Hệ đào tạo",
           avatar as "Ảnh đại diện"
    FROM Profile
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;

-- SELECT * FROM LoadProfileById('21521601');

CREATE OR REPLACE FUNCTION GetScheduleByID(_id VARCHAR(100))
RETURNS TABLE("Mã môn học" VARCHAR(100), "Tên môn học" VARCHAR(100), "Phòng học" VARCHAR(100), "Ngày bắt đầu" DATE, "Ngày kết thúc" DATE, "Thứ" VARCHAR(100), "Tiết" VARCHAR(100)) AS $$
BEGIN
    RETURN QUERY
    SELECT Course.id as "Mã môn học",
           Course.name as "Tên môn học",
           Course.classroom as "Phòng học",
           Course.startDay as "Ngày bắt đầu",
           Course.endDay as "Ngày kết thúc",
           Course.schoolDay as "Thứ",
           Course.lesson as "Tiết"
    FROM Schedule, Course
    WHERE Schedule.idProfile = _id
        AND Schedule.idCourse = Course.id;
END;
$$ LANGUAGE plpgsql;

-- SELECT * FROM GetScheduleByID('GV1');

CREATE OR REPLACE FUNCTION GetListRegisterCourse()
RETURNS TABLE("Tên môn học" VARCHAR(100), "Mã lớp" VARCHAR(100), "Tên giảng viên" VARCHAR(100), "Số tín chỉ" INT, "Thứ" VARCHAR(100), "Tiết" VARCHAR(100), "Phòng" VARCHAR(100), "Học kì" VARCHAR(100), "Năm học" VARCHAR(100), "Ngày bắt đầu" DATE, "Ngày kết thúc" DATE) AS $$
BEGIN
    RETURN QUERY
    SELECT Course.name as "Tên môn học",
           Course.id as "Mã lớp",
           Profile.name as "Tên giảng viên",
           Course.numberOfCredits as "Số tín chỉ",
           Course.schoolDay as "Thứ",
           Course.lesson as "Tiết",
           Course.classroom as "Phòng",
           Course.semester as "Học kì",
           Course.schoolYear as "Năm học",
           Course.startDay as "Ngày bắt đầu",
           Course.endDay as "Ngày kết thúc"
    FROM Schedule
    JOIN Course ON Schedule.idCourse = Course.id
    JOIN Profile ON Schedule.idProfile = Profile.id
    JOIN UserAcc ON Profile.id = UserAcc.idProfile
    JOIN Account ON UserAcc.idAccount = Account.username
    WHERE Account.role = 'teacher'
    ORDER BY Course.name, Course.id;
END;
$$ LANGUAGE plpgsql;

-- SELECT * FROM GetListRegisterCourse();

CREATE OR REPLACE FUNCTION GetLearningOutcomes(_id VARCHAR(100))
RETURNS TABLE("Mã học phần" VARCHAR(100), "Tên học phần" VARCHAR(100), "Tín chỉ" INT, "Điểm quá trình" FLOAT, "Điểm giữa kì" FLOAT, "Điểm thực hành" FLOAT, "Điểm cuối kì" FLOAT, "Điểm học phần" FLOAT, "Học kì" VARCHAR(100), "Năm học" VARCHAR(100)) AS $$
BEGIN
    RETURN QUERY
    SELECT Course.id as "Mã học phần",
           Course.name as "Tên học phần",
           Course.numberOfCredits as "Tín chỉ",
           Score.processScore as "Điểm quá trình",
           Score.midtermScore as "Điểm giữa kì",
           Score.practiceScore as "Điểm thực hành",
           Score.finalScore as "Điểm cuối kì",
		   cast((Score.processScore * Score.ratioProcess 
               + Score.midtermScore * Score.ratioMidterm 
               + Score.practiceScore * Score.ratioPractice
               + Score.finalScore * Score.ratioFinal) as numeric(10, 2))::float as "Điểm học phần",
           Course.semester as "Học kì",
           Course.schoolYear as "Năm học"
    FROM Schedule
    JOIN Course ON Schedule.idCourse = Course.id
    JOIN Score ON Schedule.idScore = Score.id
    WHERE Schedule.idProfile = _id;
END;
$$ LANGUAGE plpgsql;

-- SELECT * FROM GetLearningOutcomes('21521601');

CREATE OR REPLACE FUNCTION GetClassInCharge(_id VARCHAR(100))
RETURNS TABLE("Mã lớp" VARCHAR(100), "Tên môn học" VARCHAR(100), "Thứ" VARCHAR(100), "Tiết" VARCHAR(100), "SLSV" BIGINT, "Ghi chú" VARCHAR(100)) AS $$
BEGIN
    RETURN QUERY
    SELECT Course.id as "Mã lớp",
           Course.name as "Tên môn học",
           Course.schoolDay as "Thứ",
           Course.lesson as "Tiết",
           COUNT(*) as "SLSV",
           Schedule.note as "Ghi chú"
    FROM Schedule
    JOIN Course ON Schedule.idCourse = Course.id
    WHERE NOT Schedule.idProfile = _id
      AND Schedule.idCourse IN (
        SELECT Course.id
        FROM Schedule
        JOIN Course ON Schedule.idCourse = Course.id
        WHERE Schedule.idProfile = _id
      )
    GROUP BY Course.id, Course.name, Course.schoolDay, Course.lesson, Schedule.note;
END;
$$ LANGUAGE plpgsql;

-- SELECT * FROM GetClassInCharge('GV2');

CREATE OR REPLACE FUNCTION GetListClass(_idCourse VARCHAR(100))
RETURNS TABLE("MSSV" VARCHAR(100), "Tên sinh viên" VARCHAR(100), "Điểm quá trình" FLOAT, "Điểm giữa kì" FLOAT, "Điểm thực hành" FLOAT, "Điểm cuối kì" FLOAT, "Điểm học phần" FLOAT) AS $$
BEGIN
    RETURN QUERY
    SELECT Profile.id as "MSSV",
           Profile.name as "Tên sinh viên",
           Score.processScore as "Điểm quá trình",
           Score.midtermScore as "Điểm giữa kì",
           Score.practiceScore as "Điểm thực hành",
           Score.finalScore as "Điểm cuối kì",
           cast((Score.processScore * Score.ratioProcess 
               + Score.midtermScore * Score.ratioMidterm 
               + Score.practiceScore * Score.ratioPractice
               + Score.finalScore * Score.ratioFinal) as numeric(10, 2))::float as "Điểm học phần"
    FROM Schedule
    JOIN Profile ON Schedule.idProfile = Profile.id
    JOIN Score ON Schedule.idScore = Score.id
    WHERE Schedule.idCourse = _idCourse
    ORDER BY Profile.id;
END;
$$ LANGUAGE plpgsql;

-- SELECT * FROM GetListClass('IT003.1');


-------------------------
------ CRUD -----------
CREATE OR REPLACE FUNCTION InsertAcc(_username VARCHAR(100), _password VARCHAR(1000), _id VARCHAR(100))
RETURNS VOID AS $$
BEGIN
    BEGIN

        INSERT INTO Account(username, password)
        VALUES (_username, _password);

        INSERT INTO Profile(id)
        VALUES (_id);

        INSERT INTO UserAcc(idAccount, idProfile)
        VALUES (_username, _id);


    EXCEPTION
        WHEN OTHERS THEN
            RAISE NOTICE 'An error occurred: %', SQLERRM;
    END;
END;
$$ LANGUAGE plpgsql;

-- SELECT InsertAcc('student0', '123456', '21521600');

CREATE OR REPLACE FUNCTION UpdatePass(_username VARCHAR(100), _password VARCHAR(1000))
RETURNS VOID AS $$
BEGIN
    UPDATE Account
    SET password = _password
    WHERE username = _username;
END;
$$ LANGUAGE plpgsql;

-- SELECT UpdatePass('student0', '654321');

CREATE OR REPLACE FUNCTION UpdateProfile(_id VARCHAR(100), _name VARCHAR(100), _birthday DATE, _gender VARCHAR(100), _level VARCHAR(100), _trainingSystem VARCHAR(100), _avatar BYTEA)
RETURNS VOID AS $$
BEGIN
    UPDATE Profile
    SET name = _name,
        birthday = _birthday,
        gender = _gender,
        level = _level,
        trainingsystem = _trainingSystem,
        avatar = _avatar
    WHERE id = _id;
END;
$$ LANGUAGE plpgsql;

-- SELECT UpdateProfile('21521600', 'Học sinh 0', '2000-01-01', 'Nam', 'Đại học', 'Chính quy', NULL);

CREATE OR REPLACE FUNCTION UpdateScore(_idCourse VARCHAR(100), _idProfile VARCHAR(100), _processScore FLOAT, _midtermScore FLOAT, _finalScore FLOAT, _practiceScore FLOAT)
RETURNS VOID AS $$
BEGIN
    UPDATE Score AS S
    SET processScore = _processScore,
        midtermScore = _midtermScore,
        finalScore = _finalScore,
        practiceScore = _practiceScore
    FROM Schedule AS Sch
    WHERE Sch.idScore = S.id
        AND Sch.idProfile = _idProfile
        AND Sch.idCourse = _idCourse;
END;
$$ LANGUAGE plpgsql;

-- SELECT UpdateScore('IT003.1', '21521600', 8.3, 7.1, 9.2, 10.0);

CREATE OR REPLACE FUNCTION UpdateRatioScore(_idCourse VARCHAR(100), _ratioProcess FLOAT, _ratioMidterm FLOAT, _ratioFinal FLOAT, _ratioPractice FLOAT)
RETURNS VOID AS $$
BEGIN
    IF _ratioProcess > 1 OR _ratioMidterm > 1 OR _ratioFinal > 1 OR _ratioPractice > 1 THEN
        RETURN;
    END IF;

    UPDATE Score AS S
    SET ratioProcess = _ratioProcess,
        ratioMidterm = _ratioMidterm,
        ratioFinal = _ratioFinal,
        ratioPractice = _ratioPractice
    FROM Schedule AS Sch
    WHERE Sch.idCourse = _idCourse
        AND Sch.idScore = S.id;
END;
$$ LANGUAGE plpgsql;

-- SELECT UpdateRatioScore('IT003.1', 0.2, 0.3, 0.5, 0);

CREATE OR REPLACE FUNCTION JoinCourse(_idProfile VARCHAR(100), _idCourse VARCHAR(100))
RETURNS VOID AS $$
DECLARE
    _idScore INT;
BEGIN
    IF (SELECT COUNT(*) FROM Schedule
        WHERE idProfile = _idProfile AND idCourse = _idCourse) > 0 THEN
        RETURN;
    END IF;

    SELECT INTO _idScore id FROM Score
    WHERE id = (SELECT MAX(id) FROM Score);

    INSERT INTO Score (processScore) VALUES (NULL);

    INSERT INTO Schedule (idCourse, idProfile, idScore)
    VALUES (_idCourse, _idProfile, _idScore + 1);
END;
$$ LANGUAGE plpgsql;

-- SELECT JoinCourse('21521600', 'IT003.1');

CREATE OR REPLACE FUNCTION LeaveCourse(_idProfile VARCHAR(100), _idCourse VARCHAR(100))
RETURNS VOID AS $$
DECLARE
    _idScore INT;
BEGIN
    SELECT INTO _idScore Score.id FROM Score, Schedule
    WHERE Schedule.idScore = Score.id
        AND Schedule.idProfile = _idProfile
        AND Schedule.idCourse = _idCourse;

    DELETE FROM Schedule
    WHERE Schedule.idScore = _idScore;

    DELETE FROM Score
    WHERE id = _idScore;
END;
$$ LANGUAGE plpgsql;

-- SELECT LeaveCourse('21521600', 'IT003.1');

-- Insert data

-- Chèn dữ liệu vào bảng "Profile" cho học sinh
DO $$ 
DECLARE 
    i INT := 1;
    gender VARCHAR(100);
BEGIN
    WHILE i <= 10 LOOP
        IF random() * 10 < 5 THEN
            gender := 'Nam';
        ELSE
            gender := 'Nữ';
        END IF;

        INSERT INTO Profile(id, name, gender)
        VALUES ((i + 21521600)::VARCHAR, 'Student ' || i::VARCHAR, gender);
        
        i := i + 1;
    END LOOP;
END $$;

-- Chèn dữ liệu vào bảng "Profile" cho giáo viên
INSERT INTO Profile(id, name)
VALUES ('GV1', 'Teacher 1');

INSERT INTO Profile(id, name)
VALUES ('GV2', 'Teacher 2');

INSERT INTO Profile(id, name)
VALUES ('GV3', 'Teacher 3');

-- Chèn dữ liệu vào bảng "Profile" cho quản lý
INSERT INTO Profile(id, name)
VALUES ('QL', 'Manager 1');



-- Chèn dữ liệu vào bảng "Account" cho quản lý
INSERT INTO Account(username, password, role)
VALUES ('admin', '123456', 'admin');

-- Chèn dữ liệu vào bảng "Account" cho giáo viên
INSERT INTO Account(username, password, role)
VALUES ('teacher1', '123456', 'teacher');

INSERT INTO Account(username, password, role)
VALUES ('teacher2', '123456', 'teacher');

INSERT INTO Account(username, password, role)
VALUES ('teacher3', '123456', 'teacher');

-- Chèn dữ liệu vào bảng "Account" cho học sinh
DO $$ 
DECLARE 
    i INT := 1;
BEGIN
    WHILE i <= 10 LOOP
        INSERT INTO Account(username, password)
        VALUES ('student' || i::VARCHAR, '123456');
        i := i + 1;
    END LOOP;
END $$;


-- Chèn dữ liệu vào bảng "UserAcc" cho học sinh
DO $$ 
DECLARE 
    i INT := 1;
BEGIN
    WHILE i <= 10 LOOP
        INSERT INTO UserAcc(idAccount, idProfile)
        VALUES ('student' || i::VARCHAR, (i + 21521600)::VARCHAR);
        i := i + 1;
    END LOOP;
END $$;

-- Chèn dữ liệu vào bảng "UserAcc" cho giáo viên
DO $$ 
DECLARE 
    i INT := 1;
BEGIN
    WHILE i <= 3 LOOP
        INSERT INTO UserAcc(idAccount, idProfile)
        VALUES ('teacher' || i::VARCHAR, 'GV' || i::VARCHAR);
        i := i + 1;
    END LOOP;
END $$;

-- Chèn dữ liệu vào bảng "UserAcc" cho quản lý
INSERT INTO UserAcc(idAccount, idProfile)
VALUES ('admin', 'QL');

-- Course


-- IT001
INSERT into Course(id, name, startDay, endDay, schoolDay, lesson ,classroom, numberOfCredits, semester, schoolYear)
VALUES (N'IT001.1', N'Nhập môn lập trình', '2023-09-11', '2023-12-30', 2, N'12345', N'B2.22', 4, N'HK1', N'2023-2024');

INSERT into Course(id, name, startDay, endDay, schoolDay, lesson ,classroom, numberOfCredits, semester, schoolYear)
VALUES (N'IT001.2', N'Nhập môn lập trình', '2023-09-11', '2023-12-30', 2, N'6789', N'B5.02', 4, N'HK1', N'2023-2024');

INSERT into Course(id, name, startDay, endDay, schoolDay, lesson ,classroom, numberOfCredits, semester, schoolYear)
VALUES (N'IT001.3', N'Nhập môn lập trình', '2023-09-11', '2023-12-30', 4, N'1234', N'B1.22', 4, N'HK1', N'2023-2024');

INSERT into Course(id, name, startDay, endDay, schoolDay, lesson ,classroom, numberOfCredits, semester, schoolYear)
VALUES (N'IT001.4', N'Nhập môn lập trình', '2023-09-11', '2023-12-30', 4, N'67890', N'B3.02', 4, N'HK1', N'2023-2024');

-- IT003
INSERT into Course(id, name, startDay, endDay, schoolDay, lesson ,classroom, numberOfCredits, semester, schoolYear)
VALUES (N'IT003.1', N'Cấu trúc dữ liệu và giải thuật', '2023-09-11', '2023-12-30', 6, N'12345', N'B3.18', 4, N'HK1', N'2023-2024');

INSERT into Course(id, name, startDay, endDay, schoolDay, lesson ,classroom, numberOfCredits, semester, schoolYear)
VALUES (N'IT003.2', N'Cấu trúc dữ liệu và giải thuật', '2023-09-11', '2023-12-30', 3, N'67890', N'B3.08', 4, N'HK1', N'2023-2024');

INSERT into Course(id, name, startDay, endDay, schoolDay, lesson ,classroom, numberOfCredits, semester, schoolYear)
VALUES (N'IT003.3', N'Cấu trúc dữ liệu và giải thuật', '2023-09-11', '2023-12-30', 5, N'6789', N'B5.02', 4, N'HK1', N'2023-2024');

CREATE OR REPLACE FUNCTION random_between(low INT ,high INT) 
   RETURNS INT AS
$$
BEGIN
   RETURN cast((random()* (high-low + 1) + low) as numeric(10, 2));
END;
$$ language 'plpgsql' STRICT;


DO $$
DECLARE
    i INT := 0;
    min_score int := 5;
    max_score int := 10;
BEGIN
    WHILE i < 20 LOOP
        INSERT INTO Score(processScore, midtermScore, finalScore, practiceScore)
        VALUES (random_between(min_score, max_score),
                random_between(min_score, max_score),
				random_between(min_score, max_score),
				random_between(min_score, max_score));
        i := i + 1;
    END LOOP;
END $$;


-- Schedule

-- Teacher

INSERT into Schedule(idCourse, idProfile)
VALUES (N'IT001.2', N'GV1');
INSERT into Schedule(idCourse, idProfile)
VALUES (N'IT001.3', N'GV1');
INSERT into Schedule(idCourse, idProfile)
VALUES (N'IT003.3', N'GV1');

INSERT into Schedule(idCourse, idProfile)
VALUES (N'IT001.1', N'GV2');
INSERT into Schedule(idCourse, idProfile)
VALUES (N'IT001.4', N'GV2');

INSERT into Schedule(idCourse, idProfile)
VALUES (N'IT003.1', N'GV3');
INSERT into Schedule(idCourse, idProfile)
VALUES (N'IT003.2', N'GV3');

DO $$ 
DECLARE
    idScore INT := 1;
    i INT := 1;
BEGIN
    WHILE i <= 10 LOOP
        IF i % 3 = 1 THEN
            INSERT INTO Schedule(idCourse, idProfile, idScore) 
            VALUES ('IT001.2', (21521600 + i)::VARCHAR, idScore),
                   ('IT003.1', (21521600 + i)::VARCHAR, idScore + 1);
            idScore := idScore + 2;
        ELSIF i % 3 = 2 THEN
            INSERT INTO Schedule(idCourse, idProfile, idScore) 
            VALUES ('IT001.4', (21521600 + i)::VARCHAR, idScore),
                   ('IT003.2', (21521600 + i)::VARCHAR, idScore + 1);
            idScore := idScore + 2;
        ELSE
            INSERT INTO Schedule(idCourse, idProfile, idScore) 
            VALUES ('IT001.1', (21521600 + i)::VARCHAR, idScore),
                   ('IT003.3', (21521600 + i)::VARCHAR, idScore + 1);
            idScore := idScore + 2;
        END IF;
        i := i + 1;
    END LOOP;
END $$;

UPDATE Schedule
SET idCourse = 'IT001.3'
WHERE idProfile = '21521609' OR idCourse = '21521604';

select * from Account;
select * from Profile;
select * from UserAcc;
select * from Course;
select * from Score;
select * from Schedule;