-- phpMyAdmin SQL Dump
-- version 5.0.2
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Creato il: Giu 06, 2020 alle 17:59
-- Versione del server: 10.4.11-MariaDB
-- Versione PHP: 7.4.4

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `db_wifi-localization`
--

-- --------------------------------------------------------

--
-- Struttura della tabella `pacchetti`
--

CREATE TABLE `pacchetti` (
  `ID_pacchetto` int(11) NOT NULL,
  `MAC` varchar(20) NOT NULL,
  `RSSI` int(11) NOT NULL,
  `SSID` varchar(35) DEFAULT NULL,
  `timestamp` int(11) NOT NULL,
  `hash` varchar(20) NOT NULL,
  `ID_scheda` int(11) NOT NULL,
  `global` tinyint(1) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Struttura della tabella `posizioni`
--

CREATE TABLE `posizioni` (
  `ID_posizione` int(11) NOT NULL,
  `MAC` varchar(20) CHARACTER SET utf8 NOT NULL,
  `x` double NOT NULL,
  `y` double NOT NULL,
  `timestamp` int(11) NOT NULL,
  `global` tinyint(1) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Struttura della tabella `schede`
--

CREATE TABLE `schede` (
  `ID_scheda` int(11) NOT NULL,
  `x` double NOT NULL DEFAULT 0,
  `y` double NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Dump dei dati per la tabella `schede`
--

INSERT INTO `schede` (`ID_scheda`, `x`, `y`) VALUES
(1, 23, 32),
(2, 26, 29);

--
-- Indici per le tabelle scaricate
--

--
-- Indici per le tabelle `pacchetti`
--
ALTER TABLE `pacchetti`
  ADD PRIMARY KEY (`ID_pacchetto`);

--
-- Indici per le tabelle `posizioni`
--
ALTER TABLE `posizioni`
  ADD PRIMARY KEY (`ID_posizione`) USING BTREE;

--
-- Indici per le tabelle `schede`
--
ALTER TABLE `schede`
  ADD PRIMARY KEY (`ID_scheda`);

--
-- AUTO_INCREMENT per le tabelle scaricate
--

--
-- AUTO_INCREMENT per la tabella `pacchetti`
--
ALTER TABLE `pacchetti`
  MODIFY `ID_pacchetto` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `posizioni`
--
ALTER TABLE `posizioni`
  MODIFY `ID_posizione` int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
