-- phpMyAdmin SQL Dump
-- version 5.0.2
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Creato il: Giu 06, 2020 alle 17:57
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
-- Database: `mydb`
--

-- --------------------------------------------------------

--
-- Struttura della tabella `boards`
--

CREATE TABLE `boards` (
  `idBoard` int(11) NOT NULL,
  `x` float NOT NULL DEFAULT 0,
  `y` float NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Dump dei dati per la tabella `boards`
--

INSERT INTO `boards` (`idBoard`, `x`, `y`) VALUES
(1, 0, 0),
(2, 10, 0);

-- --------------------------------------------------------

--
-- Struttura della tabella `pacchetti`
--

CREATE TABLE `pacchetti` (
  `ID` int(11) NOT NULL,
  `MAC` varchar(20) NOT NULL,
  `RSSI` int(11) NOT NULL,
  `SSID` varchar(35) DEFAULT NULL,
  `TIMESTAMP` int(11) NOT NULL,
  `HASH` varchar(20) NOT NULL,
  `IDSCHEDA` int(11) NOT NULL,
  `Global` tinyint(1) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Struttura della tabella `posizioni`
--

CREATE TABLE `posizioni` (
  `IDposizioni` int(11) NOT NULL,
  `MAC` varchar(20) CHARACTER SET utf8 NOT NULL,
  `X` double NOT NULL,
  `Y` double NOT NULL,
  `TIMESTAMP` int(11) NOT NULL,
  `Global` tinyint(1) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Indici per le tabelle scaricate
--

--
-- Indici per le tabelle `boards`
--
ALTER TABLE `boards`
  ADD PRIMARY KEY (`idBoard`);

--
-- Indici per le tabelle `pacchetti`
--
ALTER TABLE `pacchetti`
  ADD PRIMARY KEY (`ID`);

--
-- Indici per le tabelle `posizioni`
--
ALTER TABLE `posizioni`
  ADD PRIMARY KEY (`IDposizioni`) USING BTREE;

--
-- AUTO_INCREMENT per le tabelle scaricate
--

--
-- AUTO_INCREMENT per la tabella `pacchetti`
--
ALTER TABLE `pacchetti`
  MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT per la tabella `posizioni`
--
ALTER TABLE `posizioni`
  MODIFY `IDposizioni` int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
