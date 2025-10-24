# ClarkAI (Payment Module)

This repository contains the Payment Module of the ClarkAI Backend, built with .NET 8.
It powers the Paystack-based subscription flow for the [ClarkAI Frontend](https://github.com/segunojo1/clarkai-fe).

## Overview

The ClarkAI Payment Module does all processes related to payment initialization, verification, and auto subscription.

## Features

* **Paystack Integration** – payment initialization and verification using Paystack’s API
* **Subscription Lifecycle** – activate, renew, and cancel subscriptions securely
* **Duplicate Handling** – concurrency-safe insertions with Postgres constraints
* **WebHook** – to auto verify payment and do the required stuffs
* **Error-Handled Retry Mechanism** – transient DB exceptions handled gracefully

## Tech Stack

* **Language**: C#
* **Framework**: .NET 8 Web API
* **Database**: PostgreSQL
* **Payment Gateway**: Paystack
* **Architecture**: Onion

## My Contribution

I implemented the **payment subsystem**, including:

* Integration with Paystack for initializing and verifying payments
* Subscription validation and renewal logic
* Handling duplicate reference conflicts via Postgres constraints
* Asynchronous transaction safety with Unit of Work
* API endpoints for initializing and verifying payments
* Webhook that verifies the payment after payment automatically
* And a job that runs

---

## 🎥 Demo Video

Watch the full demo here:
[View on Google Drive](https://drive.google.com/file/d/1ORATdmL-N5CY347z-KG3wGNetKJHs8OE/view?usp=sharing)

