# Installation

## Installation de Python

Installer la dernière version de Python (3.7.2 au moment de l'écriture de ce fichier)

## Préparation de l'environnement

Pour créer votre environnement virtuel de travail, installer l'outil python virtualenv

`pip install virtualenv`

Cet outil va nous permettre d'isoler les packages python nécessaire au lab sans impacter d'autres programmes python de votre machine. Nous pouvons créer un environnement de travail maintenant:

`virtualenv twitterlab`

Un dossier twitterlab va apparaitre dans le dossier Python. Vous pouvez maintenant activer l'environnement: 

Pour windows: 
`.\twitterlab\Scripts\activate`

Pour Mac:
`source twitterlab/bin/activate`

Vous pouvez maintenant installer les packages Python nécessaire au lab:

`pip install -r requirements.txt`
