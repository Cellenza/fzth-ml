// Databricks notebook source
// MAGIC %md Import du package des fonctions

// COMMAND ----------

import org.apache.spark.sql.functions._

// COMMAND ----------

// MAGIC %md Récupération des paramètres d'entrée:
// MAGIC * Nom du compte de stockage
// MAGIC * Clé du compte de stockage
// MAGIC * Container d'entrée
// MAGIC * Container de sortie

// COMMAND ----------

// MAGIC %md Pour exécuter le notebook dans Databricks: 
// MAGIC 1) Commentez la cellule 6
// MAGIC 2) Décommentez la cellule 5
// MAGIC 3) Renseigner les bonnes valeurs

// COMMAND ----------

/*val storageAccountName = "fzthdemosto"
val storageAccountKey = "OFQYnHpAkOx+lKnFh2o9OqVVhmIN2EpMBqTMtwnVeVsi14DRfPlenDdpAJAUaayznn8Op8DTrjKGHWQroqd1nA=="

val inputContainer = "input"
val outputContainer = "output"
val filename = "tweets.json"*/

// COMMAND ----------

val storageAccountName = dbutils.widgets.get("storageAccountName")
val storageAccountKey = dbutils.widgets.get("storageAccountKey")

val inputContainer = dbutils.widgets.get("inputContainer")
val outputContainer = dbutils.widgets.get("outputContainer")
val filename = dbutils.widgets.get("filename")

// COMMAND ----------

// MAGIC %md Connexion au container d'entrée du compte de stockage

// COMMAND ----------

if (!dbutils.fs.mounts.map(mnt => mnt.mountPoint).contains("/mnt/input"))
  dbutils.fs.mount(
    source = s"wasbs://$inputContainer@$storageAccountName.blob.core.windows.net/",
    mountPoint = "/mnt/input",
    extraConfigs = Map(s"fs.azure.account.key.$storageAccountName.blob.core.windows.net" -> storageAccountKey))

// COMMAND ----------

// MAGIC %md Chargement en mémoire des fichiers de tweets au format json

// COMMAND ----------

val rawTweetsDF = spark.read.json("/mnt/input/tweets*.json.gz")
display(rawTweetsDF.groupBy("StatusID").count().agg(sum("count")))

// COMMAND ----------

// MAGIC %md Sélectionne uniquement les champs utiles pour l'étape de ML

// COMMAND ----------

val cleanTweetsDF = rawTweetsDF.select(
                          col("CreatedAt"), 
                          col("Text"), 
                          col("StatusID")
                        )

display(cleanTweetsDF.groupBy("StatusID").count().agg(sum("count").alias("NbTweets")))

// COMMAND ----------

// MAGIC %md Supprime les doublons de Tweets par rapport au champs StatusID

// COMMAND ----------

val uniqTweetsDF = cleanTweetsDF.dropDuplicates("StatusID")
display(uniqTweetsDF.groupBy("StatusID").count().agg(sum("count").alias("NbTweets")))

// COMMAND ----------

// MAGIC %md Connexion au container de sortie du compte de stockage

// COMMAND ----------

if (!dbutils.fs.mounts.map(mnt => mnt.mountPoint).contains("/mnt/output"))
  dbutils.fs.mount(
    source = s"wasbs://$outputContainer@$storageAccountName.blob.core.windows.net/",
    mountPoint = "/mnt/output",
    extraConfigs = Map(s"fs.azure.account.key.$storageAccountName.blob.core.windows.net" -> storageAccountKey))

// COMMAND ----------

// MAGIC %md Sauvegarde du dataframe nettoyé dans le container de sortie

// COMMAND ----------

uniqTweetsDF    
  .write 
  .mode("overwrite")
  .json("/mnt/output")  

// COMMAND ----------

// MAGIC %md Spark produit n fichiers JSON préfixés avec "part-"<br>
// MAGIC Ces fichiers sont au format JSON lines => une ligne correspond à un JSON<br>
// MAGIC La web app prend en entrée un seul fichier JSON multiligne<br>
// MAGIC Ci-dessous, concaténation des différents fichiers part-*.json en un seul fichier JSON au format multiligne<br>

// COMMAND ----------

import java.io._
import scala.io.Source

// Méthode permettant de lister les fichiers d'un répertoire
def getListOfFiles(dir: String):List[File] = {
    val d = new File(dir)
    if (d.exists && d.isDirectory) {
        d.listFiles.filter(_.isFile).toList
    } else {
        List[File]()
    }
}

// Concaténation des fichiers part dans le fichier temporaire
val outputPath = "/dbfs/mnt/output"
val filePrefix = "part"
val filepath = outputPath + "/" + filename

val fileDest = new File(filepath)
fileDest.createNewFile()
val bw = new BufferedWriter(new FileWriter(fileDest))

val partFiles = getListOfFiles(outputPath).filter(_.getName.startsWith(filePrefix))
bw.write("[")

// Parcourt tous les fichiers part*
for ((file, fileIndex) <- partFiles.zipWithIndex) {
  val lines = Source.fromFile(file.getCanonicalPath).getLines()
  // Parcourt des lignes du fichier
  for ((line, lineIndex) <- lines.zipWithIndex) {
    // Si c'est la dernière ligne du dernier fichier, pas de virgule en fin de ligne
    if (fileIndex == partFiles.size-1 && lines.isEmpty) {
      bw.write(line)
    }
    else {
      bw.write(line + ",")
    }
  }
}

bw.write("]")
bw.close()

// COMMAND ----------


