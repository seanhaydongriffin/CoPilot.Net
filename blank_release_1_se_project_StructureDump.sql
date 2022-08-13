-- MySQL dump 10.13  Distrib 8.0.28, for Win64 (x86_64)
--
-- Host: localhost    Database: blank_release_1_se_project
-- ------------------------------------------------------
-- Server version	5.7.37-log

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `case`
--

DROP TABLE IF EXISTS `case`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `case` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) DEFAULT NULL,
  `archived` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `custom_field`
--

DROP TABLE IF EXISTS `custom_field`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `custom_field` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(45) NOT NULL,
  `value` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=206 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `environment`
--

DROP TABLE IF EXISTS `environment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `environment` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(45) NOT NULL,
  `external_id` varchar(11) DEFAULT NULL,
  `archived` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=23 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `execution_group`
--

DROP TABLE IF EXISTS `execution_group`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `execution_group` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `iteration_id` int(11) NOT NULL,
  `environment_id` int(11) NOT NULL,
  `scheduled` varchar(13) DEFAULT NULL,
  `auto_stop_tests` int(11) DEFAULT NULL,
  `auto_send_results` int(11) DEFAULT NULL,
  `device_notifications` int(11) DEFAULT NULL,
  `number_of_repeats` int(11) DEFAULT NULL,
  `start_date_time` datetime DEFAULT NULL,
  `end_date_time` datetime DEFAULT NULL,
  `result` varchar(11) DEFAULT NULL,
  `external_id` varchar(11) DEFAULT NULL,
  `external_plan_id` int(11) DEFAULT NULL,
  `external_plan_name` varchar(100) DEFAULT NULL,
  `external_exe_rec_run_id` int(11) DEFAULT NULL,
  `external_exe_rec_run_name` varchar(100) DEFAULT NULL,
  `archived` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=364 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `execution_group_script`
--

DROP TABLE IF EXISTS `execution_group_script`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `execution_group_script` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `execution_group_id` int(11) NOT NULL,
  `order_id` int(11) NOT NULL,
  `script_id` int(11) NOT NULL,
  `machine_id` int(11) NOT NULL,
  `selector` varchar(2) NOT NULL DEFAULT 'S',
  `post_run_delay` varchar(15) DEFAULT NULL,
  `cpu_threshold` int(3) NOT NULL DEFAULT '100',
  `start_date_time` datetime DEFAULT NULL,
  `end_date_time` datetime DEFAULT NULL,
  `state` varchar(15) DEFAULT NULL,
  `state_override` varchar(15) DEFAULT NULL,
  `first_exception_message` varchar(2000) DEFAULT NULL,
  `failure_count` int(4) DEFAULT NULL,
  `excluded` tinyint(1) DEFAULT '0',
  `em_comment` varchar(256) DEFAULT NULL,
  `custom Defect Owner` int(11) DEFAULT NULL,
  `custom Defect Number` varchar(15) DEFAULT NULL,
  `custom Defect Root Cause` int(11) DEFAULT NULL,
  `custom Customer Number` varchar(128) DEFAULT NULL,
  `custom Account Number` varchar(128) DEFAULT NULL,
  `custom Entitlement Number` varchar(128) DEFAULT NULL,
  `custom Sales Order Number` varchar(128) DEFAULT NULL,
  `custom Service ID` varchar(128) DEFAULT NULL,
  `custom External Batch ID` varchar(128) DEFAULT NULL,
  `shared_folder_1` varchar(260) DEFAULT NULL,
  `elevated` tinyint(1) DEFAULT '0',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=14269 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `execution_group_script_log`
--

DROP TABLE IF EXISTS `execution_group_script_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `execution_group_script_log` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `execution_group_script_id` int(11) NOT NULL,
  `date_time` datetime NOT NULL,
  `result` varchar(11) NOT NULL,
  `text` varchar(2000) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=22377194 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `iteration`
--

DROP TABLE IF EXISTS `iteration`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `iteration` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `external_id` varchar(11) DEFAULT NULL,
  `archived` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=65 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `script`
--

DROP TABLE IF EXISTS `script`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `script` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `description` varchar(128) DEFAULT NULL,
  `comment` varchar(256) DEFAULT NULL,
  `external_id` varchar(11) DEFAULT NULL,
  `archived` tinyint(1) NOT NULL DEFAULT '0',
  `custom Owner` int(11) DEFAULT NULL,
  `custom Status` int(11) DEFAULT NULL,
  `custom S No` varchar(4) DEFAULT NULL,
  `custom Channel` int(11) DEFAULT NULL,
  `custom LOB` int(11) DEFAULT NULL,
  `custom Data` int(11) DEFAULT NULL,
  `custom Journey` int(11) DEFAULT NULL,
  `custom Priority after Review` int(11) DEFAULT NULL,
  `custom Top Priority` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=4915 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2022-08-13 22:12:20
