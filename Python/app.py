# Etape 0  : Import des bibliothèques importantes 

import pandas as pd
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.neighbors import NearestNeighbors

# Etape 1 : Chargement du fichier json grace à Pandas

df = pd.read_json("tweets.json")
print(df.head())


# Etape 2 : Afficher une vue d'ensemble de chaque colonne
#           ainsi que la taille en mémoire du fichier  

print(df.info())


# Etape 3 : Vectorisation du contenu des tweets
#           use_idf : Indique si l'on souhaite utiliser l'Inverse Document Frequency
#           analyser : Le type de tokenization que l'on souhaite (char ou word)
#           ngram_range : Un tuple indiquant la taille min et max d'un token (fenetre coulissante)

contentVectorizer = TfidfVectorizer(use_idf=True,analyzer='char',ngram_range=(3,3))


# Etape 4 : Application de la vectorisation sur la colonne Text de notre fichier

vectors = contentVectorizer.fit_transform(df.Text)

# Etape 5 : Creation du model Machine Learning non supervisé et declenchement de l'apprentissage
#           n_neighbors : indique que nous voulions un top 3
#           metric : La fonction de mesure de distance que l'on souhaite, ici Cosine Distance
#           algorithm : Brute force (la fonction a brute forcer est celle qui reduit la distance du cosinus)

model = NearestNeighbors(n_neighbors=3,algorithm='brute',metric='cosine')
model.fit(vectors)


# Etape 6 : Test du model

text_to_search = ["On rencontre le 2"]
vector_to_search = contentVectorizer.transform(text_to_search)
distances, indices = model.kneighbors(vector_to_search)
binomes = list(zip(indices[0],distances[0]))


# Etape 7 : affichage des résultats 


for pos, (indice,distance) in enumerate(binomes):

    item = df.iloc[indice]
    print(pos+1,"-"," ",item.Text, " ",(distance))
