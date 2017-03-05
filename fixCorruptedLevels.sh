for f in $(find . -type f); do
if (tail -n 1 $f | python -m json.tool > /dev/null); then
:
else
tail -n 1 "$f" | wc -c | xargs -I {} truncate "$f" -s -{}
fi
done

echo "Removing empty files and directories..."
find . -type f -empty -print -delete
find . -type d -empty -print -delete

echo "Deleting backup files..."
find . -type f -name "sed*" -print -delete
find . -type f -name "OW*" -print -delete
